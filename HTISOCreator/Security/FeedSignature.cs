using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace HTISOCreator.Security
{
    /// <summary>Public keys trusted for advisory metadata. The set is compiled into the program, so it can't be swapped next to it.</summary>
    public sealed class TrustedKeySet
    {
        private readonly Dictionary<string, byte[]> _keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public TrustedKeySet(IEnumerable<KeyValuePair<string, string>> keyIdAndSpki)
        {
            foreach (KeyValuePair<string, string> k in keyIdAndSpki)
            {
                byte[] spki;
                try
                {
                    spki = Convert.FromBase64String(k.Value);
                }
                catch (FormatException)
                {
                    throw new CryptographicException("Key " + k.Key + " is not valid base64.");
                }

                // Throws unless this is an uncompressed ECDSA P-256 SubjectPublicKeyInfo.
                FeedSignature.PointFromSpki(spki);
                _keys[k.Key] = spki;
            }
        }

        public int Count
        {
            get { return _keys.Count; }
        }

        public static TrustedKeySet Load(byte[] trustedKeysJson)
        {
            return new TrustedKeySet(StrictJson.ReadTrustedKeys(trustedKeysJson));
        }

        internal bool TryGet(string keyId, out byte[] spki)
        {
            return _keys.TryGetValue(keyId, out spki);
        }
    }

    public enum SignatureCheck
    {
        Valid,
        Missing,
        UnknownKey,
        Invalid,
        Malformed
    }

    /// <summary>
    /// ECDSA P-256 / SHA-256 detached signatures over the exact file bytes (IEEE P1363 r||s encoding, base64),
    /// verified with Windows CNG, which is available from Windows 7 onwards.
    /// </summary>
    public static class FeedSignature
    {
        public const string Algorithm = "ECDSA-P256-SHA256";

        // DER prefix of an ECDSA P-256 SubjectPublicKeyInfo: SEQUENCE { SEQUENCE { id-ecPublicKey, prime256v1 }, BIT STRING 04||X||Y }.
        private static readonly byte[] SpkiPrefix =
        {
            0x30, 0x59, 0x30, 0x13, 0x06, 0x07, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01, 0x06, 0x08, 0x2A,
            0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07, 0x03, 0x42, 0x00, 0x04
        };

        private const int BcryptEcdsaPublicP256Magic = 0x31534345; // "ECS1"

        public static SignatureCheck Verify(byte[] content, byte[] signatureJson, TrustedKeySet trusted)
        {
            if (signatureJson == null || signatureJson.Length == 0)
            {
                return SignatureCheck.Missing;
            }

            DetachedSignature sig;
            try
            {
                sig = StrictJson.ReadSignature(signatureJson);
            }
            catch (AdvisoryFormatException)
            {
                return SignatureCheck.Malformed;
            }

            if (sig.Algorithm != Algorithm || string.IsNullOrEmpty(sig.KeyId) || sig.Signature == null)
            {
                return SignatureCheck.Malformed;
            }

            byte[] spki;
            if (!trusted.TryGet(sig.KeyId, out spki))
            {
                return SignatureCheck.UnknownKey;
            }

            byte[] raw;
            try
            {
                raw = Convert.FromBase64String(sig.Signature);
            }
            catch (FormatException)
            {
                return SignatureCheck.Malformed;
            }

            if (raw.Length != 64)
            {
                return SignatureCheck.Malformed;
            }

            using (CngKey key = CngKey.Import(EccPublicBlob(spki), CngKeyBlobFormat.EccPublicBlob))
            using (var ecdsa = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha256 })
            {
                return ecdsa.VerifyData(content, raw) ? SignatureCheck.Valid : SignatureCheck.Invalid;
            }
        }

        /// <summary>The 64-byte X||Y public point of a P-256 SubjectPublicKeyInfo.</summary>
        internal static byte[] PointFromSpki(byte[] spki)
        {
            if (spki == null || spki.Length != SpkiPrefix.Length + 64 || !spki.Take(SpkiPrefix.Length).SequenceEqual(SpkiPrefix))
            {
                throw new CryptographicException("The key is not an uncompressed ECDSA P-256 public key.");
            }

            return spki.Skip(SpkiPrefix.Length).ToArray();
        }

        /// <summary>SubjectPublicKeyInfo (base64) for a CNG P-256 key, as stored in trusted-keys.json.</summary>
        internal static string SpkiFromCngKey(CngKey key)
        {
            byte[] blob = key.Export(CngKeyBlobFormat.EccPublicBlob);
            if (blob.Length != 8 + 64 || BitConverter.ToInt32(blob, 0) != BcryptEcdsaPublicP256Magic)
            {
                throw new CryptographicException("The key is not an ECDSA P-256 key.");
            }

            return Convert.ToBase64String(SpkiPrefix.Concat(blob.Skip(8)).ToArray());
        }

        private static byte[] EccPublicBlob(byte[] spki)
        {
            byte[] point = PointFromSpki(spki);
            var blob = new byte[8 + 64];
            BitConverter.GetBytes(BcryptEcdsaPublicP256Magic).CopyTo(blob, 0);
            BitConverter.GetBytes(32).CopyTo(blob, 4);
            point.CopyTo(blob, 8);
            return blob;
        }
    }
}

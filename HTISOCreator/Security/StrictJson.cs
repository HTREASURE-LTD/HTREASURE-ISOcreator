using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace HTISOCreator.Security
{
    /// <summary>Advisory data that is not valid JSON, or does not match the schema's shape.</summary>
    public sealed class AdvisoryFormatException : Exception
    {
        public AdvisoryFormatException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Strict JSON reading for advisory data, matching the standards' System.Text.Json options: camelCase names,
    /// unknown members rejected (the schema forbids them, and silently dropping data hides tampering), no comments,
    /// no trailing commas, depth limited to 32. Advisory data is only ever read as data.
    /// </summary>
    public static class StrictJson
    {
        public const int MaxBytes = 5 * 1024 * 1024;

        public static object Parse(byte[] utf8)
        {
            string text;
            try
            {
                text = new UTF8Encoding(false, true).GetString(utf8);
            }
            catch (ArgumentException)
            {
                throw new AdvisoryFormatException("The data is not valid UTF-8 text.");
            }

            if (text.Length > 0 && text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            string syntaxProblem = ForbiddenSyntax(text);
            if (syntaxProblem != null)
            {
                throw new AdvisoryFormatException("The data is not valid JSON: " + syntaxProblem);
            }

            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = MaxBytes * 2, RecursionLimit = 32 };
                return serializer.DeserializeObject(text);
            }
            catch (ArgumentException ex)
            {
                throw new AdvisoryFormatException("The data is not valid JSON: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new AdvisoryFormatException("The data is not valid JSON: " + ex.Message);
            }
        }

        public static AdvisoryFeed ReadFeed(byte[] utf8)
        {
            JsonObject o = JsonObject.From(Parse(utf8), "feed");
            var feed = new AdvisoryFeed
            {
                FeedType = o.String("feedType", true),
                SchemaVersion = o.String("schemaVersion", true),
                Kind = o.String("kind", true),
                Sequence = o.Long("sequence", true),
                GeneratedAt = o.Date("generatedAt", true).Value,
                ExpiresAt = o.Date("expiresAt", false),
                Scope = o.String("scope", true),
                Advisories = o.Array("advisories", true, ReadAdvisory)
            };
            o.RejectUnknown();
            return feed;
        }

        public static SecurityAdvisory ReadAdvisory(byte[] utf8)
        {
            return ReadAdvisory(Parse(utf8), "advisory");
        }

        public static DetachedSignature ReadSignature(byte[] utf8)
        {
            JsonObject o = JsonObject.From(Parse(utf8), "signature");
            var sig = new DetachedSignature
            {
                Algorithm = o.String("algorithm", false),
                KeyId = o.String("keyId", false),
                Signature = o.String("signature", false)
            };
            o.RejectUnknown();
            return sig;
        }

        /// <summary>Reads security/keys/trusted-keys.json: { "description": "...", "keys": [ { "keyId", "publicKeySpki" } ] }.</summary>
        public static IList<KeyValuePair<string, string>> ReadTrustedKeys(byte[] utf8)
        {
            JsonObject o = JsonObject.From(Parse(utf8), "trusted keys");
            o.String("description", false);
            IList<KeyValuePair<string, string>> keys = o.Array("keys", true, (value, path) =>
            {
                JsonObject k = JsonObject.From(value, path);
                var pair = new KeyValuePair<string, string>(k.String("keyId", true), k.String("publicKeySpki", true));
                k.RejectUnknown();
                return pair;
            });
            o.RejectUnknown();
            return keys;
        }

        private static SecurityAdvisory ReadAdvisory(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var a = new SecurityAdvisory
            {
                SchemaVersion = o.String("schemaVersion", false) ?? "1.0.0",
                Id = o.String("id", true),
                AppId = o.String("appId", true),
                Title = o.String("title", true),
                Summary = o.String("summary", true),
                Severity = o.String("severity", true),
                Cvss = o.Object("cvss", false, ReadCvss),
                Aliases = o.Array("aliases", false, ReadAlias),
                Published = o.String("published", true),
                Updated = o.String("updated", true),
                VerificationStatus = o.String("verificationStatus", true),
                Affected = o.Array("affected", true, ReadAffected),
                FixedVersions = o.Array("fixedVersions", false, AsString),
                Remediation = o.String("remediation", true),
                Mitigation = o.String("mitigation", false),
                References = o.Array("references", false, ReadReference),
                ReleaseNotes = o.String("releaseNotes", false)
            };
            o.RejectUnknown();
            return a;
        }

        private static CvssInfo ReadCvss(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var c = new CvssInfo { Version = o.String("version", true), Vector = o.String("vector", true), BaseScore = o.Double("baseScore", true) };
            o.RejectUnknown();
            return c;
        }

        private static AdvisoryAlias ReadAlias(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var a = new AdvisoryAlias { Type = o.String("type", true), Id = o.String("id", true), Verified = o.Bool("verified", true) };
            o.RejectUnknown();
            return a;
        }

        private static AdvisoryReference ReadReference(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var r = new AdvisoryReference { Type = o.String("type", true), Url = o.String("url", true) };
            o.RejectUnknown();
            return r;
        }

        private static AffectedEntry ReadAffected(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var e = new AffectedEntry
            {
                Platforms = o.Array("platforms", true, AsString),
                Channels = o.Array("channels", true, AsString),
                Status = o.String("status", true),
                Ranges = o.Array("ranges", false, ReadRange),
                FixedBuilds = o.Array("fixedBuilds", false, AsString),
                AssessedThrough = o.String("assessedThrough", false)
            };
            o.RejectUnknown();
            return e;
        }

        private static VersionRange ReadRange(object value, string path)
        {
            JsonObject o = JsonObject.From(value, path);
            var r = new VersionRange { Introduced = o.String("introduced", true), Fixed = o.String("fixed", false), LastAffected = o.String("lastAffected", false) };
            o.RejectUnknown();
            return r;
        }

        private static string AsString(object value, string path)
        {
            var s = value as string;
            if (s == null)
            {
                throw new AdvisoryFormatException(path + " must be a string.");
            }

            return s;
        }

        // JavaScriptSerializer is more lenient than the standard's reader: it accepts comments, trailing commas and
        // single-quoted strings. Reject them, looking only outside double-quoted strings.
        internal static string ForbiddenSyntax(string text)
        {
            bool inString = false;
            int lastSignificant = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                        lastSignificant = i;
                    }

                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '/')
                {
                    return "comments are not allowed.";
                }
                else if (c == '\'')
                {
                    return "single-quoted strings are not allowed.";
                }
                else if ((c == '}' || c == ']') && lastSignificant >= 0 && text[lastSignificant] == ',')
                {
                    return "trailing commas are not allowed.";
                }

                lastSignificant = i;
            }

            return null;
        }

        /// <summary>One JSON object, read member by member; any member not read is reported as unknown.</summary>
        private sealed class JsonObject
        {
            private readonly IDictionary<string, object> _values;
            private readonly string _path;
            private readonly HashSet<string> _read = new HashSet<string>(StringComparer.Ordinal);

            private JsonObject(IDictionary<string, object> values, string path)
            {
                _values = values;
                _path = path;
            }

            public static JsonObject From(object value, string path)
            {
                var dict = value as IDictionary<string, object>;
                if (dict == null)
                {
                    throw new AdvisoryFormatException(path + " must be a JSON object.");
                }

                return new JsonObject(dict, path);
            }

            public void RejectUnknown()
            {
                string unknown = _values.Keys.FirstOrDefault(k => !_read.Contains(k));
                if (unknown != null)
                {
                    throw new AdvisoryFormatException(_path + " has an unknown member '" + unknown + "'.");
                }
            }

            public string String(string name, bool required)
            {
                object value = Get(name, required);
                if (value == null)
                {
                    return null;
                }

                var s = value as string;
                if (s == null)
                {
                    throw new AdvisoryFormatException(_path + "." + name + " must be a string.");
                }

                return s;
            }

            public bool Bool(string name, bool required)
            {
                object value = Get(name, required);
                if (!(value is bool))
                {
                    throw new AdvisoryFormatException(_path + "." + name + " must be true or false.");
                }

                return (bool)value;
            }

            public long Long(string name, bool required)
            {
                object value = Get(name, required);
                if (value is int)
                {
                    return (int)value;
                }

                if (value is long)
                {
                    return (long)value;
                }

                throw new AdvisoryFormatException(_path + "." + name + " must be a whole number.");
            }

            public double Double(string name, bool required)
            {
                object value = Get(name, required);
                if (value is int || value is long || value is decimal || value is double)
                {
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }

                throw new AdvisoryFormatException(_path + "." + name + " must be a number.");
            }

            public DateTimeOffset? Date(string name, bool required)
            {
                string s = String(name, required);
                if (s == null)
                {
                    return null;
                }

                DateTimeOffset result;
                if (!DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result) || s.IndexOf('T') < 0)
                {
                    throw new AdvisoryFormatException(_path + "." + name + " must be an ISO 8601 date and time.");
                }

                return result;
            }

            public T Object<T>(string name, bool required, Func<object, string, T> read)
                where T : class
            {
                object value = Get(name, required);
                return value == null ? null : read(value, _path + "." + name);
            }

            public IList<T> Array<T>(string name, bool required, Func<object, string, T> read)
            {
                object value = Get(name, required);
                if (value == null)
                {
                    return null;
                }

                var items = value as IEnumerable;
                if (items == null || value is string || value is IDictionary<string, object>)
                {
                    throw new AdvisoryFormatException(_path + "." + name + " must be an array.");
                }

                var list = new List<T>();
                int i = 0;
                foreach (object item in items)
                {
                    list.Add(read(item, _path + "." + name + "[" + i++ + "]"));
                }

                return list;
            }

            private object Get(string name, bool required)
            {
                _read.Add(name);
                object value;
                if (!_values.TryGetValue(name, out value) || value == null)
                {
                    if (required)
                    {
                        throw new AdvisoryFormatException(_path + " is missing the required member '" + name + "'.");
                    }

                    return null;
                }

                return value;
            }
        }
    }

    /// <summary>Small indented JSON writer for security reports.</summary>
    internal sealed class JsonTextWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int _indent;
        private bool _first = true;

        public override string ToString()
        {
            return _sb.ToString();
        }

        public JsonTextWriter BeginObject(string name = null)
        {
            Prefix(name);
            _sb.Append('{');
            _indent++;
            _first = true;
            return this;
        }

        public JsonTextWriter EndObject()
        {
            _indent--;
            NewLine();
            _sb.Append('}');
            _first = false;
            return this;
        }

        public JsonTextWriter BeginArray(string name = null)
        {
            Prefix(name);
            _sb.Append('[');
            _indent++;
            _first = true;
            return this;
        }

        public JsonTextWriter EndArray()
        {
            _indent--;
            NewLine();
            _sb.Append(']');
            _first = false;
            return this;
        }

        public JsonTextWriter Value(string name, string value)
        {
            Prefix(name);
            if (value == null)
            {
                _sb.Append("null");
            }
            else
            {
                Quote(value);
            }

            return this;
        }

        public JsonTextWriter Value(string name, bool value)
        {
            Prefix(name);
            _sb.Append(value ? "true" : "false");
            return this;
        }

        public JsonTextWriter Value(string name, DateTimeOffset? value)
        {
            return Value(name, value.HasValue ? value.Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture) : null);
        }

        private void Prefix(string name)
        {
            if (!_first)
            {
                _sb.Append(',');
            }

            if (_sb.Length > 0)
            {
                NewLine();
            }

            _first = false;
            if (name != null)
            {
                Quote(name);
                _sb.Append(": ");
            }
        }

        private void NewLine()
        {
            _sb.Append('\n').Append(' ', _indent * 2);
        }

        private void Quote(string s)
        {
            _sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':
                        _sb.Append("\\\"");
                        break;
                    case '\\':
                        _sb.Append("\\\\");
                        break;
                    case '\n':
                        _sb.Append("\\n");
                        break;
                    case '\r':
                        _sb.Append("\\r");
                        break;
                    case '\t':
                        _sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _sb.Append(c);
                        }

                        break;
                }
            }

            _sb.Append('"');
        }
    }
}

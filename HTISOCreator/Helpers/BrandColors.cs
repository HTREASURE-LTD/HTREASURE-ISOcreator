using System.Drawing;
using HTISOCreator.Security;

namespace HTISOCreator.Helpers
{
    /// <summary>
    /// HTREASURE LTD corporate palette (branding\brand.json), plus the severity and status colours of the HTREASURE
    /// software standards' Security Advisory History screen.
    /// </summary>
    public static class BrandColors
    {
        public static readonly Color DeepNavy = ColorTranslator.FromHtml("#0B1F3A");
        public static readonly Color TechnologyBlue = ColorTranslator.FromHtml("#0B3D91");
        public static readonly Color ElectricBlue = ColorTranslator.FromHtml("#168BFF");
        public static readonly Color SilverGrey = ColorTranslator.FromHtml("#AAB4C3");
        public static readonly Color Edge = ColorTranslator.FromHtml("#3A5275");
        public static readonly Color PageGrey = ColorTranslator.FromHtml("#F4F6F9");
        public static readonly Color LineGrey = ColorTranslator.FromHtml("#DDE3EB");
        public static readonly Color MutedText = ColorTranslator.FromHtml("#5A6577");

        public static readonly Color Red = ColorTranslator.FromHtml("#B42318");
        public static readonly Color Orange = ColorTranslator.FromHtml("#C4320A");
        public static readonly Color Amber = ColorTranslator.FromHtml("#B54708");
        public static readonly Color Green = ColorTranslator.FromHtml("#067647");
        public static readonly Color Slate = ColorTranslator.FromHtml("#475467");
        public static readonly Color AmberBackground = ColorTranslator.FromHtml("#FFF6E5");
        public static readonly Color BlueBackground = ColorTranslator.FromHtml("#EAF3FF");
        public static readonly Color RedBackground = ColorTranslator.FromHtml("#FEF3F2");

        public static Color For(AdvisorySeverity severity)
        {
            switch (severity)
            {
                case AdvisorySeverity.Critical:
                    return Red;
                case AdvisorySeverity.High:
                    return Orange;
                case AdvisorySeverity.Medium:
                    return Amber;
                default:
                    return TechnologyBlue;
            }
        }

        public static Color For(AdvisoryStatus status)
        {
            switch (status)
            {
                case AdvisoryStatus.Affected:
                    return Red;
                case AdvisoryStatus.UnderReview:
                    return Amber;
                case AdvisoryStatus.Unknown:
                    return Slate;
                default:
                    return Green;
            }
        }
    }
}

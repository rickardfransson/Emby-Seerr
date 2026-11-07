using System;
using MediaBrowser.Model.Logging;
using SkiaSharp;

namespace Bestall.Platshallare
{
    /// <summary>
    /// Lägger ett "BESTÄLL"-band nederst på omslaget, så att platshållare inte förväxlas med filmer ni har.
    /// Använder SkiaSharp som följer med Emby. Saknas det, eller misslyckas ritningen, används omslaget som det är.
    /// </summary>
    public sealed class Omslagsmarkering
    {
        private readonly ILogger _log;
        private bool _otillganglig;

        public Omslagsmarkering(ILogger log) => _log = log;

        public byte[] Markera(byte[] original)
        {
            if (_otillganglig) return original;
            try
            {
                return Rita(original) ?? original;
            }
            catch (Exception ex) when (ex is TypeLoadException || ex is DllNotFoundException || ex is System.IO.FileNotFoundException || ex is TypeInitializationException)
            {
                _otillganglig = true;
                _log.Warn("SkiaSharp finns inte tillgängligt; platshållarnas omslag får inget band. ({0})", ex.Message);
                return original;
            }
            catch (Exception ex)
            {
                _log.Debug("Kunde inte markera omslag: {0}", ex.Message);
                return original;
            }
        }

        // Får inte bakas in i Markera: då skulle ett saknat SkiaSharp kasta innan try-blocket.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static byte[] Rita(byte[] original)
        {
            using (var bild = SKBitmap.Decode(original))
            {
                if (bild == null) return null;

                using (var yta = SKSurface.Create(new SKImageInfo(bild.Width, bild.Height)))
                {
                    var c = yta.Canvas;
                    c.DrawBitmap(bild, 0, 0);

                    var bandHojd = bild.Height * 0.11f;
                    var bandTopp = bild.Height - bandHojd;

                    using (var band = new SKPaint { Color = new SKColor(0xFC, 0xA3, 0x11, 235), IsAntialias = true })
                        c.DrawRect(0, bandTopp, bild.Width, bandHojd, band);

                    using (var typsnitt = SKTypeface.FromFamilyName(null, SKFontStyle.Bold))
                    using (var text = new SKPaint
                    {
                        Color = new SKColor(0x14, 0x21, 0x3D),
                        IsAntialias = true,
                        Typeface = typsnitt,
                        TextSize = bandHojd * 0.55f,
                        TextAlign = SKTextAlign.Center
                    })
                    {
                        var grans = new SKRect();
                        text.MeasureText("BESTÄLL", ref grans);
                        c.DrawText("BESTÄLL", bild.Width / 2f, bandTopp + bandHojd / 2f - grans.MidY, text);
                    }

                    using (var resultat = yta.Snapshot())
                    using (var data = resultat.Encode(SKEncodedImageFormat.Jpeg, 90))
                        return data.ToArray();
                }
            }
        }
    }
}

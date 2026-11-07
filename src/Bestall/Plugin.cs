using System;
using System.Collections.Generic;
using System.IO;
using Bestall.Configuration;
using Bestall.Kanal;
using Bestall.Seerr;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Bestall
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public SeerrKlient Seerr { get; }
        public SeerrStatus SeerrStatus { get; }
        public Tittarregister Tittare { get; }
        public Forslagscache Forslag { get; }

        public string BestalldVideo { get; }
        public string EjTillgangligVideo { get; }

        public Plugin(IApplicationPaths paths, IXmlSerializer xml, ILogManager logManager) : base(paths, xml)
        {
            Instance = this;
            var log = logManager.GetLogger("Bestall");

            Seerr = new SeerrKlient(() => Configuration, log);
            SeerrStatus = new SeerrStatus(Seerr);
            Tittare = new Tittarregister(Seerr, log);
            Forslag = new Forslagscache(Seerr, () => Configuration, log);

            var mapp = Path.Combine(paths.PluginConfigurationsPath, "Bestall");
            Directory.CreateDirectory(mapp);
            BestalldVideo = PackaUpp("Bestall.Media.bestalld.mp4", Path.Combine(mapp, "bestalld.mp4"));
            EjTillgangligVideo = PackaUpp("Bestall.Media.ej-tillganglig.mp4", Path.Combine(mapp, "ej-tillganglig.mp4"));
        }

        public override string Name => "Beställ film";
        public override string Description => "Låter tittare beställa filmer via Seerr direkt från Embys TV-appar.";
        public override Guid Id => new Guid("a3c5e0d2-7b41-4c8e-9f16-5d2b8e4a7c90");

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            base.UpdateConfiguration(configuration);
            SeerrStatus.Glom();
            Tittare.Glom();
            Forslag.Glom();
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo { Name = "bestall", EmbeddedResourcePath = "Bestall.Configuration.configPage.html", IsMainConfigPage = true },
            new PluginPageInfo { Name = "bestalljs", EmbeddedResourcePath = "Bestall.Configuration.configPage.js" }
        };

        private string PackaUpp(string resurs, string mal)
        {
            using (var kalla = GetType().Assembly.GetManifestResourceStream(resurs))
            using (var fil = File.Create(mal))
                kalla.CopyTo(fil);
            return mal;
        }
    }
}

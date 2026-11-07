using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

namespace Bestall.Platshallare
{
    /// <summary>Schemalagd uppgift i Emby som håller urvalet aktuellt. Körs varje natt och går att starta för hand.</summary>
    public class Synkuppgift : IScheduledTask
    {
        private readonly ILibraryMonitor _bibliotek;
        private readonly ILogger _log;

        public Synkuppgift(ILibraryMonitor bibliotek, ILogManager logManager)
        {
            _bibliotek = bibliotek;
            _log = logManager.GetLogger("Bestall");
        }

        public string Name => "Uppdatera platshållare för beställning";
        public string Key => "BestallPlatshallarsynk";
        public string Description => "Hämtar beställningsbara filmer från Seerr och uppdaterar platshållarbiblioteket.";
        public string Category => "Beställ film";

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var p = Plugin.Instance;
            var synk = new Platshallarsynk(p.Seerr, p.Forslag, () => p.Configuration, _log);

            p.Forslag.Glom();
            var andrat = await synk.KorAsync(progress, cancellationToken).ConfigureAwait(false);

            if (andrat)
                _bibliotek.ReportFileSystemChanged(p.Configuration.PlatshallarMapp);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
        {
            new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerDaily, TimeOfDayTicks = TimeSpan.FromHours(4).Ticks }
        };
    }
}

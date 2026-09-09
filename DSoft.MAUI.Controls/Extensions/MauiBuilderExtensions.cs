using DSoft.Maui.Controls.TouchTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui.Hosting
{
    public static class MauiBuilderExtensions
    {
        public static MauiAppBuilder UseDSoftControls(this MauiAppBuilder builder)
        {
            builder.ConfigureEffects(effects =>
            {
                effects.Add<TouchEffect, TouchPlatformEffect>();
                effects.Add<ScrollCaptureEffect, ScrollCapturePlatformEffect>();

            });

            return builder;
        }
    }
}

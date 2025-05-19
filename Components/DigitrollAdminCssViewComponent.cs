using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Components
{
    public class DigitrollAdminCssViewComponent : NopViewComponent
    {
        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            if (widgetZone.Equals(AdminWidgetZones.HeaderBefore, StringComparison.InvariantCultureIgnoreCase))
            {
                return View("~/Plugins/Payments.Dintero/Views/DigitrollAdminScripts.cshtml");
            }
            return Content(string.Empty);
        }
    }
}

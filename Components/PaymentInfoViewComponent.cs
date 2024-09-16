using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Dintero.Components;

[ViewComponent(Name = Defaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
public class PaymentInfoViewComponent : NopViewComponent
{
    #region Methods

    public IViewComponentResult Invoke()
    {
        return View("~/Plugins/Payments.Dintero/Views/PaymentInfo.cshtml");
    }

    #endregion
}

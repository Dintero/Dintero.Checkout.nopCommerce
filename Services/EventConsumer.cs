using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Payments.Dintero.Services;
public class EventConsumer : IConsumer<AdminMenuCreatedEvent>
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IAdminMenu _adminMenu;
   

    #endregion

    #region Ctor

    public EventConsumer(ILocalizationService localizationService, IAdminMenu adminMenu)
    {
        _localizationService = localizationService;
        _adminMenu = adminMenu;
    }

    #endregion

    #region Methods

    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        var adminMenuItem = new AdminMenuItem()
        {
            SystemName = "Payments.Dintero",
            Title = "Dintero Checkout",
            IconClass = "far fa-dot-circle",
        };
        eventMessage.RootMenuItem.ChildNodes.Add(adminMenuItem);

        var configureMenu = new AdminMenuItem()
        {
            SystemName = "Payments.Dintero",
            Title = "Configuration",
            IconClass = "far fa-dot-circle",
            Url = _adminMenu.GetMenuItemUrl("Dintero", "Configure")
        };
        adminMenuItem.ChildNodes.Add(configureMenu);
    }

    #endregion
}

import $ from "jquery";
import * as dintero from "@dintero/checkout-web-sdk";

const container = document.getElementById("checkout-container");
var paymentselected = false;
$(document).ready(function () {
    $(".header").addClass("mb-0");
    displayAjaxLoading(true);
    if ($("#sandboxDinteroCheckoutWebSDKEndpoint").val() == false) {
        dintero
            .embed({
                container,
                sid: $("#dinteroOrderSessionResponseId").val(),
                popOut: true,
                language: "no",
                onSession: function (event, checkout) {
                    if (event['type'] == 'SessionUpdated') {
                        checkout.lockSession();
                        displayAjaxLoading(true);
                        $.ajax({
                            cache: false,
                            url: '/DinteroCheckout/SaveAddressesShippingAddress',
                            type: 'POST',
                            data: { dinteroSessionId: event.session.id }
                        }).done(function (result) {
                            if (result.error >= 0) {
                                if (AjaxCart.usepopupnotifications === true) {
                                    displayPopupNotification(result.message, 'error', true);
                                }
                                else {
                                    //no timeout for errors
                                    displayBarNotification(result.message, 'error', 0);
                                }
                                $("#refreshbtn").show();
                                displayAjaxLoading(false);
                                checkout.destroy();
                            } else {
                                if (result.orderSummary) {
                                    $("#dinteroOrderSummary").html(result.orderSummary);
                                }
                                displayAjaxLoading(false);
                                checkout.refreshSession();
                            }
                        }).fail(function (jqXHR, exception) {
                            if (AjaxCart.usepopupnotifications === true) {
                                displayPopupNotification("Something went wrong", 'error', true);
                            }
                            else {
                                //no timeout for errors
                                displayBarNotification("Something went wrong", 'error', 0);
                            }
                            $("#refreshbtn").show();
                            displayAjaxLoading(false);
                            checkout.destroy();
                        });
                    }
                    displayAjaxLoading(false);
                },
                onPayment: function (event, checkout) {
                    checkout.destroy();
                    displayAjaxLoading(true);
                    // replce test url with local for testing
                    window.location.href = event.href;
                },
                onPaymentError: function (event, checkout) {
                    $("#refreshbtn").show();
                    checkout.destroy();
                },
                onSessionCancel: function (event, checkout) {
                    window.location.href = "/cart";
                },
                onSessionNotFound: function (event, checkout) {
                    $("#refreshbtn").show();
                    checkout.destroy();
                },
                onActivePaymentType: function (event, checkout) {
                },
                onValidateSession: function (event, checkout, callback) {
                    try {
                        displayAjaxLoading(true);
                        $.ajax({
                            cache: false,
                            url: '/DinteroCheckout/SavePaymentMethod',
                            type: 'POST',
                            data: {
                                dinteroSessionId: checkout.options.sid
                            }
                        }).done(function (result) {
                            if (result.error >= 0) {
                                if (AjaxCart.usepopupnotifications === true) {
                                    displayPopupNotification(result.message, 'error', true);
                                }
                                else {
                                    //no timeout for errors
                                    displayBarNotification(result.message, 'error', 0);
                                }
                                $("#refreshbtn").show();
                                displayAjaxLoading(false);
                                checkout.destroy();
                                callback({
                                    success: false,
                                    clientValidationError: "Something went wrong: " + error.message
                                });
                            } else {
                                if (result.orderSummary) {
                                    $("#dinteroOrderSummary").html(result.orderSummary);
                                }
                                displayAjaxLoading(false);
                                checkout.refreshSession();
                                callback({
                                    success: true,
                                });
                            }
                        }).fail(function (jqXHR, exception) {
                            if (AjaxCart.usepopupnotifications === true) {
                                displayPopupNotification("Something went wrong", 'error', true);
                            }
                            else {
                                //no timeout for errors
                                displayBarNotification("Something went wrong", 'error', 0);
                            }
                            $("#refreshbtn").show();
                            displayAjaxLoading(false);
                            checkout.destroy();
                            callback({
                                success: false,
                                clientValidationError: "Something went wrong"
                            });
                        });
                    }
                    catch (err) {
                        callback({
                            success: false,
                            clientValidationError: "Something went wrong " + err.message
                        });
                    }
                },
            })
            .then(function (checkout) {
                //console.log("checkout", checkout);
            });
    } else {
        dintero
            .embed({
                container,
                sid: $("#dinteroOrderSessionResponseId").val(),
                endpoint: $("#sandboxDinteroCheckoutWebSDKEndpoint").val(),
                popOut: true,
                language: "no",
                onSession: function (event, checkout) {
                    if (event['type'] == 'SessionUpdated') {
                        checkout.lockSession();
                        displayAjaxLoading(true);
                        $.ajax({
                            cache: false,
                            url: '/DinteroCheckout/SaveAddressesShippingAddress',
                            type: 'POST',
                            data: { dinteroSessionId: event.session.id }
                        }).done(function (result) {
                            if (result.error >= 0) {
                                if (AjaxCart.usepopupnotifications === true) {
                                    displayPopupNotification(result.message, 'error', true);
                                }
                                else {
                                    //no timeout for errors
                                    displayBarNotification(result.message, 'error', 0);
                                }
                                $("#refreshbtn").show();
                                displayAjaxLoading(false);
                                checkout.destroy();
                            } else {
                                if (result.orderSummary) {
                                    $("#dinteroOrderSummary").html(result.orderSummary);
                                }
                                displayAjaxLoading(false);
                                checkout.refreshSession();
                            }
                        }).fail(function (jqXHR, exception) {
                            if (AjaxCart.usepopupnotifications === true) {
                                displayPopupNotification("Something went wrong", 'error', true);
                            }
                            else {
                                //no timeout for errors
                                displayBarNotification("Something went wrong", 'error', 0);
                            }
                            $("#refreshbtn").show();
                            displayAjaxLoading(false);
                            checkout.destroy();
                        });
                    }
                    displayAjaxLoading(false);
                },
                onPayment: function (event, checkout) {
                    checkout.destroy();
                    displayAjaxLoading(true);
                    // replce test url with local for testing
                    window.location.href = event.href;
                },
                onPaymentError: function (event, checkout) {
                    $("#refreshbtn").show();
                    checkout.destroy();
                },
                onSessionCancel: function (event, checkout) {
                    window.location.href = "/cart";
                },
                onSessionNotFound: function (event, checkout) {
                    $("#refreshbtn").show();
                    checkout.destroy();
                },
                onActivePaymentType: function (event, checkout) {
                },
                onValidateSession: function (event, checkout, callback) {
                    try {
                        displayAjaxLoading(true);
                        $.ajax({
                            cache: false,
                            url: '/DinteroCheckout/SavePaymentMethod',
                            type: 'POST',
                            data: {
                                dinteroSessionId: checkout.options.sid
                            }
                        }).done(function (result) {
                            if (result.error >= 0) {
                                if (AjaxCart.usepopupnotifications === true) {
                                    displayPopupNotification(result.message, 'error', true);
                                }
                                else {
                                    //no timeout for errors
                                    displayBarNotification(result.message, 'error', 0);
                                }
                                $("#refreshbtn").show();
                                displayAjaxLoading(false);
                                checkout.destroy();
                                callback({
                                    success: false,
                                    clientValidationError: "Something went wrong: " + error.message
                                });
                            } else {
                                if (result.orderSummary) {
                                    $("#dinteroOrderSummary").html(result.orderSummary);
                                }
                                displayAjaxLoading(false);
                                checkout.refreshSession();
                                callback({
                                    success: true,
                                });
                            }
                        }).fail(function (jqXHR, exception) {
                            if (AjaxCart.usepopupnotifications === true) {
                                displayPopupNotification("Something went wrong", 'error', true);
                            }
                            else {
                                //no timeout for errors
                                displayBarNotification("Something went wrong", 'error', 0);
                            }
                            $("#refreshbtn").show();
                            displayAjaxLoading(false);
                            checkout.destroy();
                            callback({
                                success: false,
                                clientValidationError: "Something went wrong"
                            });
                        });
                    }
                    catch (err) {
                        callback({
                            success: false,
                            clientValidationError: "Something went wrong: " + err.message
                        });
                    }
                },
            })
            .then(function (checkout) {
                //console.log("checkout", checkout);
            });
    }
});
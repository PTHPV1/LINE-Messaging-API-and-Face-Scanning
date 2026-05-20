
Imports System.Net
Imports System.Web.Routing

Public Class Global_asax
    Inherits HttpApplication

    Sub Application_Start(sender As Object, e As EventArgs)
        ConfigureOutboundTls()
        RegisterRoutes(RouteTable.Routes)
    End Sub

    Private Shared Sub ConfigureOutboundTls()
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        ServicePointManager.Expect100Continue = False
    End Sub

    Shared Sub RegisterRoutes(routes As RouteCollection)
        routes.MapPageRoute("home", "", "~/home.aspx")
    End Sub
End Class

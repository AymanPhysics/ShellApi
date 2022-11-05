using Microsoft.Web.WebSockets;
using System;
using System.Web;

namespace ShellApi
{
    public class WebSocket : IHttpHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: https://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(new MicrosoftWebSockets());
            }
        }

        #endregion
    }

    public class MicrosoftWebSockets : WebSocketHandler
    {
        WebSocketCollection clients = new WebSocketCollection();
        private String clientName;

        public override void OnOpen()
        {
            base.OnOpen();
            clientName = this.WebSocketContext.QueryString["clientName"];
            clients.Add(this);
            clients.Broadcast(clientName+" is connected");
        }
    }
}

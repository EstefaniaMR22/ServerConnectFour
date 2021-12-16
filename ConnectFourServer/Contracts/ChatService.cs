using Services;
using Services.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public partial class ConnectFourGameService : IChatService
    {
        private Dictionary<ColorToken, IChatServiceCallback> users = new Dictionary<ColorToken, IChatServiceCallback>();

        public void Connect(ColorToken player)
        {
            var client = OperationContext.Current.GetCallbackChannel<IChatServiceCallback>();
            ColorToken tokenFound;
            if (!FindUser(player.Player.PlayerID, out tokenFound))
            {
                users.Add(player, client);
            }
            else
            {
                tokenFound.MatchID = player.MatchID;
                users[tokenFound] = client;
            }
        }

        public void SendMessage(Message message)
        {
            var client = OperationContext.Current.GetCallbackChannel<IChatServiceCallback>();
            ColorToken token;
            if (message.Receiver > -1)
            {
                if (FindUser(message.Receiver, out token))
                {
                    IChatServiceCallback destiny;
                    users.TryGetValue(token, out destiny);
                    destiny.ReceiveMessage(message);
                }
                client.ReceiveMessage(message);
            }
            else
            {
                if (FindUser(message.Sender, out token))
                {
                    foreach (var item in users.Keys)
                    {
                        if (item.MatchID == token.MatchID)
                        {
                            IChatServiceCallback destiny;
                            users.TryGetValue(item, out destiny);
                            destiny.ReceiveMessage(message);
                        }
                    }
                }
            }
        }

        private bool FindUser(int playerID, out ColorToken player)
        {
            bool returnValue = false;
            player = null;
            foreach (var token in users)
            {
                if (token.Key.Player.PlayerID == playerID)
                {
                    player = token.Key;
                    returnValue = true;
                }
            }
            return returnValue;
        }
    }
}

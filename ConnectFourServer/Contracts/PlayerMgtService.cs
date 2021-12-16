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
    public partial class ConnectFourGameService : IPlayerMgtService
    {
        private Dictionary<IPlayerMgtServiceCallback, string> players = new Dictionary<IPlayerMgtServiceCallback, string>();

        public void SingUpPlayer(Player player)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = new Players();
                Player.email = player.Email;
                Player.nickname = player.Nickname;
                Player.password = player.Password;
                Player.biography = player.Biography;
                Player.registration = DateTime.Parse(player.Registration.ToString("d"));
                Player.profileImageName = player.ProfileIconName;
                Player.coins = player.Coins;
                Player.winnerScore = player.winnerScore;
                Player.matches = player.Matches;
                Player.status = "Offline";
                Player.lastConnection = DateTime.Now;
                context.Players.Add(Player);
                context.SaveChanges();
            }
            currentChannel.ReceivePlayerConfirmation(player);
        }

        public void UpdatePlayerInfo(Player player)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = (from p in context.Players where p.playerID == player.PlayerID select p).ToList().First();
                Player.playerID = player.PlayerID;
                Player.email = player.Email;
                Player.nickname = player.Nickname;
                Player.password = player.Password;
                Player.biography = player.Biography;
                Player.registration = DateTime.Parse(player.Registration.ToString("d"));
                Player.profileImageName = player.ProfileIconName;
                Player.coins = player.Coins;
                Player.winnerScore = player.winnerScore;
                Player.matches = player.Matches;
                context.SaveChanges();
            }
            currentChannel.ReceivePlayerConfirmation(player);
        }

        public void GetPlayerById(int playerId)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = new Players();
                Player = context.Players.Find(playerId);
                Player player1 = new Player();
                player1.PlayerID = Player.playerID;
                player1.Nickname = Player.nickname;
                player1.Registration = (DateTime)Player.registration;
                player1.Coins = (int)Player.coins;
                player1.Matches = (int)Player.matches;
                player1.winnerScore = (int)Player.winnerScore;
                player1.Biography = Player.biography;
                player1.Status = Player.status;
                player1.ProfileIconName = Player.profileImageName;
                currentChannel.ReceivePlayer(player1);
            }

        }

        public void GetPlayerByEmail(string playerEmail)
        {
            Player player1 = new Player();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = new Players();
                IEnumerable<Players> players = (from player in context.Players where player.email == playerEmail select player);
                if (players.ToList().Count != 0)
                {
                    player1.PlayerID = players.ToList()[0].playerID;
                    player1.Nickname = players.ToList()[0].nickname;
                    player1.Registration = (DateTime)players.ToList()[0].registration;
                    player1.Coins = (int)players.ToList()[0].coins;
                    player1.Matches = (int)players.ToList()[0].matches;
                    player1.winnerScore = (int)players.ToList()[0].winnerScore;
                    player1.Biography = players.ToList()[0].biography;
                    player1.ProfileIconName = players.ToList()[0].profileImageName;
                    player1.Email = players.ToList()[0].email;
                    player1.Password = players.ToList()[0].password;
                }
                else
                {
                    player1 = null;
                }
            }
                currentChannel.ReceivePlayer(player1);
        }

        public void GetPlayers()
        {
            List<Player> players = new List<Player>();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                IEnumerable<Players> Players = (from player in context.Players select player);
                if (Players.ToList().Count != 0)
                {
                    foreach (Players player in Players)
                    {
                        Player player1 = new Player();
                        player1.PlayerID = player.playerID;
                        player1.Nickname = player.nickname;
                        player1.Registration = (DateTime)player.registration;
                        player1.Coins = (int)player.coins;
                        player1.Matches = (int)player.matches;
                        player1.winnerScore = (int)player.winnerScore;
                        player1.Biography = player.biography;
                        player1.ProfileIconName = player.profileImageName;
                        player1.Email = player.email;
                        player1.Password = player.password;
                        players.Add(player1);
                    }
                }
            }
            currentChannel.ReceivePlayersList(players);
        }

        public void SendFriendRequest(FriendRequest friendRequest)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                FriendRequests friendRequests = new FriendRequests();
                friendRequests.sender = friendRequest.Sender;
                friendRequests.receiver = friendRequest.Receiver;
                friendRequests.sent = friendRequest.Sent;
                context.FriendRequests.Add(friendRequests);
                context.SaveChanges();
            }
            currentChannel.ReceiveRequestConfirmation(friendRequest);
        }

        public void AnswerFriendRequest(FriendRequest friendRequest)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            var DateCheck = new DateTime(1999, 9, 9, 9, 9, 9);
            using (var context = new ConnectFourEntities())
            {
               if (friendRequest.Sent != DateCheck)
               {
                    Friends friend1 = new Friends();
                    friend1.player = friendRequest.Sender;
                    friend1.friend = friendRequest.Receiver;
                    Friends friend2 = new Friends();
                    friend2.player = friendRequest.Receiver;
                    friend2.friend = friendRequest.Sender;
                    context.Friends.Add(friend1);
                    context.Friends.Add(friend2);
               }
               FriendRequests friendRequests = (from f in context.FriendRequests where f.sender == friendRequest.Sender && f.receiver == friendRequest.Receiver select f).ToList().First();
               context.FriendRequests.Remove(friendRequests);
               context.SaveChanges();
            }
               currentChannel.ReceiveRequestConfirmation(friendRequest);
        }

        public void GetFriends(int playerID)
        {
            List<Player> friendList = new List<Player>();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                var query = (from p in context.Players join f in context.Friends on p.playerID equals f.player where f.player == playerID select f.Players).ToList();
                foreach (var players in query)
                {
                    if (!players.playerID.Equals(playerID))
                    {
                        Player player = new Player();
                        player.ProfileIconName = players.profileImageName;
                        player.PlayerID = players.playerID;
                        player.Nickname = players.nickname;
                        friendList.Add(player);
                    } 
                }
            }
            currentChannel.ReceiveFriendList(friendList);
        }

        public void GetRequests(int playerID)
        {
            List<Player> requestList = new List<Player>();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                var query = (from p in context.Players join f in context.FriendRequests on p.playerID equals f.receiver select f.Players).ToList();
                foreach (var players in query)
                {
                    if (!players.playerID.Equals(playerID))
                    {
                        Player player = new Player();
                        player.ProfileIconName = players.profileImageName;
                        player.PlayerID = players.playerID;
                        player.Nickname = players.nickname;
                        requestList.Add(player);
                    }
                }
            }
            currentChannel.ReceiveRequestList(requestList);
        }

        public void ChangeStatus(Player player, bool status)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = (from p in context.Players where p.playerID == player.PlayerID select p).ToList().First();
                if (status)
                {
                    Player.status = "Online";                                      /* Cambiar¿*/
                }
                else
                {
                    Player.status = "Offline";
                }
                context.SaveChanges();
            }
        }

        public void GetIDFriends(int playerID)
        {
            List<int> idfriendList = new List<int>();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                var query = (from f in context.Friends where playerID == f.player select f.friend).ToList();
                foreach (var IdPlayers in query)
                {                     
                    idfriendList.Add(IdPlayers);
                }
            }
            currentChannel.ReceiveIdFriendList(idfriendList);
        }

        public void GetSendRequests(int playerID)
        {
            List<int> sendrequestList = new List<int>();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IPlayerMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                var query = (from f in context.FriendRequests where playerID == f.sender select f.receiver).ToList();
                foreach (var IdPlayers in query)
                {
                    sendrequestList.Add(IdPlayers);
                }
            }
            currentChannel.ReceiveSendRequestList(sendrequestList);
        }
    }
}

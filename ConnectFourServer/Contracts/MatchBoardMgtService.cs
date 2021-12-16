using Contracts.Model;
using Services;
using Services.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public partial class ConnectFourGameService : IMatchBoardMgtService
    {
        private Dictionary<string, int> powers = new Dictionary<string, int>()
        {
            { "push", 100 },
            { "replace", 200 }
        };

        private string[] colors = { "#FFF0F02D", "#FF52D83C", "#FFCB2828" };
        private const int coinsForWin = 50;
        private ObservableCollection<Match> matches = new ObservableCollection<Match>();

        public void CreateMatch(ColorToken token)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            Match newMatch = new Match();
            newMatch.tokens = new Dictionary<ColorToken, IMatchBoardMgtServiceCallback>();
            newMatch.GameID = GenerateGameID();
            newMatch.Start = DateTime.Now;
            Match duplicatedMatch;
            if (!FindMatchByID(newMatch.GameID, out duplicatedMatch))
            {
                token.MatchID = newMatch.GameID;
                newMatch.tokens.Add(token, currentContext);
                matches.Add(newMatch);
            }
            currentContext.NotifyGameIntegration(ToCollection(newMatch.tokens), token);                             /*If tokens.Cound == 0 : is invalid*/
        }

        public void AddPlayerInMatch(ColorToken token, int gameID)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            ObservableCollection<ColorToken> errorCollection = new ObservableCollection<ColorToken>();
            bool correctOperation = false;
            Match matchFound;
            ColorToken tokenFound;
            if (FindMatchByID(gameID, out matchFound) && matchFound.tokens.Count < 3)
            {
                if (!FindTokenByPlayerID(matchFound, gameID, out tokenFound))
                {
                    if (isFriend(matchFound, token.Player.PlayerID))
                    {
                        token.MatchID = matchFound.GameID;
                        matchFound.tokens.Add(token, currentContext);
                        correctOperation = true;
                    }
                    else
                    {
                        errorCollection = null;
                    }
                }
            }
            if (correctOperation)
            {
                foreach (var player in matchFound.tokens)
                {
                    player.Value.NotifyGameIntegration(ToCollection(matchFound.tokens), player.Key);
                }
            }
            else
            {
                currentContext.NotifyGameIntegration(errorCollection, token);                    /*If tokens.Cound == 0 : there's no enough friends == null : fatal error*/
            }
        }

        public void RemovePlayerFromMatch(ColorToken token)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            bool correctOperation = false;
            Match matchFound;
            if (FindMatchByID(token.MatchID, out matchFound))
            {
                correctOperation = DeleteToken(matchFound, token.Player.PlayerID);
                if (matchFound.tokens.Count == 0)
                {
                    DeleteMatch(matchFound.GameID);
                }
                else
                {
                    foreach (var player in matchFound.tokens)
                    {
                        player.Value.NotifyGameIntegration(ToCollection(matchFound.tokens), player.Key);
                    }
                }
            }
            currentContext.NotifyEndGameElimination(correctOperation);
        }

        public void ChangeTurn(ColorToken token, Turn turn)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            Match matchFound;
            ColorToken tokenFound;
            if (FindMatchByID(token.MatchID, out matchFound))
            {
                FindTokenByPlayerID(matchFound, token.Player.PlayerID, out tokenFound);
                ColorToken tokenNextTurn = ChangeTurnAux(matchFound, tokenFound);
                if (string.IsNullOrEmpty(token.ColorID))
                {
                    DeleteToken(matchFound, token.Player.PlayerID);
                }
                if (matchFound.tokens.Count >= 2)
                {
                    foreach (var player in matchFound.tokens)
                    {
                        player.Value.NotifyTurn(turn, tokenNextTurn);
                    }
                }
                else
                {
                    EndMatch(tokenNextTurn);
                }
            }
        }

        public void StartMatch(ColorToken token)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            Match matchFound;
            if (FindMatchByID(token.MatchID, out matchFound))
            {
                if (matchFound.tokens.Count >= 2)
                {
                    AssignColor(matchFound);
                    foreach (var player in matchFound.tokens)
                    {
                        player.Value.NotifyStartGame(player.Key, ToCollection(matchFound.tokens), colors[0]);
                    }
                }
                else
                {
                    foreach (var player in matchFound.tokens)
                    {
                        player.Value.NotifyStartGame(player.Key, ToCollection(matchFound.tokens), string.Empty);
                    }
                }
            }
        }

        public void EndMatch(ColorToken token)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            Match matchFound;
            if (FindMatchByID(token.MatchID, out matchFound))
            {
                if (matchFound.WinnerID >= 0)
                {
                    SaveMatch(matchFound, token);
                }
            }
        }

        private bool SaveMatch(Match matchForSaving, ColorToken winner)
        {
            bool returnValue = false;
            using (var context = new ConnectFourEntities())
            {
                Matches match = new Matches();
                match.gameID = matchForSaving.GameID;
                match.start = matchForSaving.Start;
                match.url = matchForSaving.GameID;
                match.end = DateTime.Now;
                match.winnerID = winner.Player.PlayerID;
                context.Matches.Add(match);

                foreach (var player in matchForSaving.tokens)
                {
                    Players playerDatabase = (from p in context.Players where p.playerID == player.Key.Player.PlayerID select p).ToList().First();
                    player.Key.Player.Matches += 1;
                    if (winner.Player.PlayerID == player.Key.Player.PlayerID)
                    {
                        player.Key.Player.Coins += coinsForWin;
                        player.Key.Player.winnerScore += 1;
                        playerDatabase.coins = player.Key.Player.Coins;
                        playerDatabase.winnerScore = player.Key.Player.winnerScore;
                    }
                    playerDatabase.matches = player.Key.Player.Matches;
                    player.Value.NotifyGameFinished(player.Key, winner.Player.Nickname);
                }
                context.SaveChanges();
                SaveMatchPlayers(matchForSaving);
                DeleteMatch(matchForSaving.GameID);
                returnValue = true;
            }
            return returnValue;
        }

        private bool SaveMatchPlayers(Match match)
        {
            bool returnValue = false;
            using (var context = new ConnectFourEntities())
            {
                Matches matchDatabase = (from m in context.Matches where m.url == match.GameID select m).ToList().First();
                foreach (var player in match.tokens.Keys)
                {
                    PlayersMatch playerMatch = new PlayersMatch();
                    playerMatch.gameID = matchDatabase.gameID;
                    playerMatch.playerID = player.Player.PlayerID;
                    context.PlayersMatch.Add(playerMatch);
                    context.SaveChanges();
                }
                returnValue = true;
            }
            return returnValue;
        }

        public void BuyPower(ColorToken token, string powerName)
        {
            var currentContext = OperationContext.Current.GetCallbackChannel<IMatchBoardMgtServiceCallback>();
            Match matchFound;
            ColorToken tokenFound;
            if (FindMatchByID(token.MatchID, out matchFound))
            {
                FindTokenByPlayerID(matchFound, token.Player.PlayerID, out tokenFound);
                if (tokenFound.Player.Coins >= powers[powerName])
                {
                    using (var context = new ConnectFourEntities())
                    {
                        Players playerDatabase = (from p in context.Players where p.playerID == tokenFound.Player.PlayerID select p).ToList().First();
                        playerDatabase.coins = tokenFound.Player.Coins - powers[powerName];
                        context.SaveChanges();
                    }
                    currentContext.NotifyPurchase(tokenFound, powerName);
                }
                else
                {
                    currentContext.NotifyPurchase(tokenFound, string.Empty);
                }
            }
        }

        //Additional function: isFriend
        private bool isFriend(Match match, int playerID)
        {
            bool returnValue = true;   
            using(var context = new ConnectFourEntities())
            {
                List<int> players = (from p in context.Friends where p.player == playerID select p.friend).ToList();
                foreach (var player in match.tokens)
                {
                    if (!players.Contains(player.Key.Player.PlayerID))
                    {
                        returnValue = false; 
                        break;
                    }
                }
            }
            return returnValue;
        }


        //Additional function: generate random key
        private int GenerateGameID()
        {
            DateTime time = DateTime.Now;
            return time.Year + time.Month + time.Day + time.Minute + (time.Second * 1000) + (time.Millisecond * 1000);
        }

        //Additional function: Find Match
        private bool FindMatchByID(int matchID, out Match matchReturned)
        {
            matchReturned = null;
            bool returnValue = false;
            foreach (var match in matches)
            {
                if (match.GameID == matchID)
                {
                    matchReturned = match;
                    returnValue = true;
                }
            }
            return returnValue;
        }

        //Additional function: Find Player token by match and context
        private bool FindTokenByPlayerID(Match match, int playerID, out ColorToken player)
        {
            player = null;
            bool returnValue = false;
            foreach (var token in match.tokens.Keys)
            {
                if (playerID == token.Player.PlayerID)
                {
                    player = token;
                    returnValue = true;
                }
            }
            return returnValue;
        }

        //Aditional function: Remove a match from the collection
        private bool DeleteMatch(int matchID)
        {
            bool returnValue = false;
            foreach (var match in matches)
            {
                if (match.GameID == matchID)
                {
                    matches.Remove(match);
                    returnValue = true;
                    break;
                }
            }
            return returnValue;
        }

        //Aditional function: Remove a token from the collection of a specific match
        private bool DeleteToken(Match match, int playerID)
        {
            bool returnValue = false;
            foreach (var token in match.tokens.Keys)
            {
                if (token.Player.PlayerID == playerID)
                {
                    match.tokens.Remove(token);
                    returnValue = true;
                    break;
                }
            }
            return returnValue;
        }

        //Aditional function: Change the turn
        private ColorToken ChangeTurnAux(Match match, ColorToken token)
        {
            ColorToken tokenReturn;
            ObservableCollection<ColorToken> tokens = new ObservableCollection<ColorToken>();
            foreach (var item in match.tokens.Keys)
            {
                tokens.Add(item);
            }
            tokenReturn = tokens[0];
            int position = tokens.IndexOf(token) + 1;
            if (position < match.tokens.Count)
            {
                tokenReturn = tokens[position];
            }
            return tokenReturn;
        }

        //Additional funcition -> Transform a Dictionary to ObservableCollection
        private ObservableCollection<ColorToken> ToCollection(Dictionary<ColorToken, IMatchBoardMgtServiceCallback> elements)
        {
            ObservableCollection<ColorToken> collection = new ObservableCollection<ColorToken>();
            foreach (var color in elements.Keys)
            {
                collection.Add(color);
            }
            return collection;
        }

        //Additinal function -> Add colorID to players
        private void AssignColor(Match match)
        {
            foreach (var player in match.tokens)
            {
                player.Key.ColorID = colors[ToCollection(match.tokens).IndexOf(player.Key)];
            }
        }
    }
}

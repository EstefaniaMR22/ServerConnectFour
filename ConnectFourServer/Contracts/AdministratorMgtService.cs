using Services;
using Services.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace Contracts
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public partial class ConnectFourGameService : IAdministratorMgtService
    {
        private Dictionary<IAdministratorMgtServiceCallback, string> administrators = new Dictionary<IAdministratorMgtServiceCallback, string>();


        public void BanPlayer(int idPlayer)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = context.Players.Find(idPlayer);
                Player.status = "Banned";
                context.Players.Attach(Player);
                context.Entry(Player).Property(x => x.status).IsModified = true;
                context.SaveChanges();

                Player player = new Player();
                player.PlayerID = Player.playerID;
                player.Nickname = Player.nickname;
                player.Registration = (DateTime)Player.registration;
                player.winnerScore = (int)Player.winnerScore;
                player.Status = Player.status;
                player.ProfileIconName = Player.profileImageName;

                currentChannel.BannedPlayerConfirmation(player);
            }
        }

        public void GetAdministratorByEmail(string email)
        {
            Administrator admin = new Administrator();
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Administrators Administrator = new Administrators();
                IEnumerable<Administrators> administrators = (from administrator in context.Administrators where administrator.email == email select administrator);
                if (administrators.ToList().Count != 0)
                {
                    admin.Email = administrators.ToList()[0].email;
                    admin.Name = administrators.ToList()[0].name;
                    admin.Biography = administrators.ToList()[0].biography;
                    admin.Password = administrators.ToList()[0].password;
                    admin.Registration = (DateTime)administrators.ToList()[0].registration;
                }
                else
                {
                    admin = null;
                }

            }
            currentChannel.ReceiveAdministrator(admin);
        }

        public void SendEmailKeyRegistration(string emailAccount)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();

            string keyRegistration = "";
            Random random = new Random();
            keyRegistration = random.Next(1000, 9999)+"";

            string body = Properties.Resources.emailSended +  " " + keyRegistration + ".";
            string sender = "connectf4uv@outlook.com";
            string displayName = Properties.Resources.nameGame;
            try
            {
                MailMessage email = new MailMessage();
                email.From = new MailAddress(sender, displayName);
                email.To.Add(emailAccount);

                email.Subject = Properties.Resources.matterEmail;
                email.Body = body;
                email.IsBodyHtml = false;

                SmtpClient client = new SmtpClient("smtp.office365.com", 587);
                client.Credentials = new NetworkCredential(sender, "conectaCuatro");
                client.EnableSsl = true;

                client.Send(email);
                currentChannel.SendEmailKeyRegistrationConfirmation(keyRegistration);
            }
            catch (Exception ex)
            {
                currentChannel.SendEmailKeyRegistrationError();
            }
        }

        public void SingUpAdministrator(Administrator admin)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Administrators administrator = new Administrators();
                administrator.name = admin.Name;
                administrator.email = admin.Email;
                administrator.password = admin.Password;
                administrator.biography = admin.Biography;
                administrator.registration = DateTime.Parse(admin.Registration.ToString("d"));
                context.Administrators.Add(administrator);
                context.SaveChanges();
            }
            currentChannel.ReceiveAdministratorConfirmation(admin);
        }

        public void UnBanPlayer(int idPlayer)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Players Player = context.Players.Find(idPlayer);
                Player.status = "Offline";
                context.Players.Attach(Player);
                context.Entry(Player).Property(x => x.status).IsModified = true;
                context.SaveChanges();

                Player player = new Player();
                player.PlayerID = Player.playerID;
                player.Nickname = Player.nickname;
                player.Registration = (DateTime)Player.registration;
                player.winnerScore = (int)Player.winnerScore;
                player.Status = Player.status;
                player.ProfileIconName = Player.profileImageName;

                currentChannel.UnBannedPlayerConfirmation(player);
            }
        }

        public void UpdateAdministratorData(Administrator admin, string email)
        {
            var currentChannel = OperationContext.Current.GetCallbackChannel<IAdministratorMgtServiceCallback>();
            using (var context = new ConnectFourEntities())
            {
                Administrators Administrator = new Administrators();
                Administrator.name = admin.Name;
                Administrator.email = admin.Email;
                Administrator.biography = admin.Biography;
                Administrator.password = admin.Password;
                Administrator.registration = admin.Registration;
                context.Entry(Administrator).State = System.Data.EntityState.Modified; 
                context.SaveChanges();
            }
            currentChannel.ReceiveAdministratorConfirmation(admin);
        }
    }
}

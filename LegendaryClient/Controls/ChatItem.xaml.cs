﻿using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using LegendaryClient.Logic;
using LegendaryClient.Properties;
using agsXMPP.protocol.client;
using agsXMPP;
using agsXMPP.Collections;
using System.Text;
using LegendaryClient.Logic.MultiUser;

namespace LegendaryClient.Controls
{
    /// <summary>
    ///     Interaction logic for ChatItem.xaml
    /// </summary>
    public partial class ChatItem
    {
        UserClient user;
        public ChatItem()
        {
            InitializeComponent();
            user = UserList.Users[Client.Current];
            MahApps.Metro.Controls.TextBoxHelper.SetWatermark(ChatTextBox, "Sending message from " + user.LoginPacket.AllSummonerData.Summoner.InternalName);
            ChatPlayerItem tempItem = null;
            var Jid = string.Empty;
            try
            {

                foreach (
                    var x in
                        Client.AllPlayers.Where(x => x.Value.Username == this.PlayerLabelName.Content))
                {
                    tempItem = x.Value;
                    Jid = x.Key + "@pvp.net";

                    break;
                }
                user.XmppConnection.MessageGrabber.Add(new Jid(Jid), new BareJidComparer(), new MessageCB(XmppConnection_OnMessage), null);
                Tag = Jid;
            }
            catch
            {
                LegendaryClient.Logic.MultiUser.Client.Log("Failed Chat");
                user.XmppConnection.OnMessage += XmppConnection_OnMessage;
            }
        }
        public void XmppConnection_OnMessage(object sender, Message msg)
        {
            if (!Client.AllPlayers.ContainsKey(msg.From.User) || string.IsNullOrWhiteSpace(msg.Body))
                return;

            var chatItem = Client.AllPlayers[msg.From.User];
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                if ((string)LegendaryClient.Logic.MultiUser.Client.ChatItem.PlayerLabelName.Content == chatItem.Username)
                    Update();
            }));
        }

        public void XmppConnection_OnMessage(object sender, Message msg, object body)
        {
            if (!Client.AllPlayers.ContainsKey(msg.From.User) || string.IsNullOrWhiteSpace(msg.Body))
                return;

            var chatItem = Client.AllPlayers[msg.From.User];
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                if ((string)LegendaryClient.Logic.MultiUser.Client.ChatItem.PlayerLabelName.Content == chatItem.Username)
                    Update();
            }));
        }

        public void Update()
        {
            ChatText.Document.Blocks.Clear();
            var tempItem =
                (from x in Client.AllPlayers
                 where x.Value.Username == (string)LegendaryClient.Logic.MultiUser.Client.ChatItem.PlayerLabelName.Content
                 select x.Value).FirstOrDefault();


            if (tempItem != null)
                for (int i = 0; i < tempItem.Messages.Count(); i++)
                {
                    var tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                    if (i > 0)
                    {

                        if (tempItem.Messages[i].time.ToShortTimeString() != tempItem.Messages[i - 1].time.ToShortTimeString() ||
                            tempItem.Messages[i].name != (tempItem.Messages[i - 1].name) || 
                            Settings.Default.AlwaysChatTimestamp)
                        {
                            if (tempItem.Messages[i].name == tempItem.Username)
                            {
                                tr.Text = tempItem.Messages[i].time.ToString("[HH:mm] ") + tempItem.Username + ": ";
                                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Turquoise);
                            }
                            else
                            {
                                tr.Text = tempItem.Messages[i].time.ToString("[HH:mm] ") + tempItem.Messages[i].name + ": ";
                                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Yellow);
                            }
                        }
                    }
                    else
                    {
                        if (tempItem.Messages[i].name == tempItem.Username)
                        {
                            tr.Text = tempItem.Messages[i].time.ToString("[HH:mm] ") + tempItem.Username + ": ";
                            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Turquoise);
                        }
                        else
                        {
                            tr.Text = tempItem.Messages[i].time.ToString("[HH:mm] ") + tempItem.Messages[i].name + ": ";
                            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Yellow);
                        }
                    }
                    tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd)
                    {
                        Text = tempItem.Messages[i].message + Environment.NewLine
                    };
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                }
            ChatText.ScrollToEnd();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatTextBox.Text))
                return;

            var tempItem =
                (from x in Client.AllPlayers
                 where x.Value.Username == (string)LegendaryClient.Logic.MultiUser.Client.ChatItem.PlayerLabelName.Content
                 select x.Value).FirstOrDefault();

            var tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
            if (tempItem.Messages.Count() == 0)
            {
                {
                    tr.Text = DateTime.Now.ToString("[HH:mm] ") + user.LoginPacket.AllSummonerData.Summoner.Name + ": ";
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Yellow);
                }

                tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd)
                {
                    Text = ChatTextBox.Text + Environment.NewLine
                };
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
            }
            else
            {
                if (tempItem.Messages.Last().name != user.LoginPacket.AllSummonerData.Summoner.Name || 
                    tempItem.Messages.Last().time.ToString("[HH:mm]") != DateTime.Now.ToString("[HH:mm]") || 
                    Settings.Default.AlwaysChatTimestamp)
                {
                    tr.Text = DateTime.Now.ToString("[HH:mm] ") + user.LoginPacket.AllSummonerData.Summoner.Name + ": ";
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Yellow);
                }

                tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd)
                {
                    Text = ChatTextBox.Text + Environment.NewLine
                };
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
            }


            var Jid = tempItem.Id + "@pvp.net";

            if (tempItem != null)
            {
                var item = new AllMessageInfo()
                {
                    message = ChatTextBox.Text,
                    time = DateTime.Now,
                    name = user.LoginPacket.AllSummonerData.Summoner.Name
                };
                tempItem.Messages.Add(item);
            }
            ChatText.ScrollToEnd();
            user.XmppConnection.Send(new Message(new Jid(Jid), MessageType.chat, ChatTextBox.Text));
            ChatTextBox.Text = string.Empty;
        }
    }
}
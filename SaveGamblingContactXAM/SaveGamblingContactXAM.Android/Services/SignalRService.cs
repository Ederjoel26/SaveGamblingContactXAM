using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using SaveGamblingContactXAM.InterfaceServices;
using SaveGamblingContactXAM.Models;
using SaveGamblingContactXAM.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Android.OS.PowerManager;
[assembly: Xamarin.Forms.Dependency(typeof(SaveGamblingContactXAM.Droid.Services.SignalRService))]
namespace SaveGamblingContactXAM.Droid.Services
{
    [Service]
    public class SignalRService : Service, ISignalRServices
    {
        private ContactModel oModel = new ContactModel();
        private CreateContact cContacto = new CreateContact();
        private bool serviceStarted = false;
        public static bool IsForegroundServiceRunning;
        private PowerManager.WakeLock wakeLock;
        private HubConnection hubConnection;
        private static SignalRService _instance;
        public static SignalRService Instance => _instance;
        private Timer reconnectTimer;
        public SignalRService()
        {
            _instance = this;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            IsForegroundServiceRunning = true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            IsForegroundServiceRunning = false;
            // Liberar el WakeLock si aún está sostenido
            if (wakeLock != null && wakeLock.IsHeld)
            {
                wakeLock.Release();
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (intent.Action == "START_SERVICE")
            {
                System.Diagnostics.Debug.WriteLine("Conexión iniciada");
                PowerManager pm = (PowerManager)GetSystemService(PowerService);
                wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "YourWakeLockTag");
                wakeLock.Acquire();
                RegisterNotification();
                if (!serviceStarted)
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        StartForegroundService(intent);
                    }
                    else
                    {
                        StartService(intent);
                    }

                    serviceStarted = true;
                }
            }
            else if (intent.Action == "STOP_SERVICE")
            {
                System.Diagnostics.Debug.WriteLine("Conexión finalizada");
                StopSelfResult(startId);
                serviceStarted = false;
                // Liberar el WakeLock
                if (wakeLock != null && wakeLock.IsHeld)
                {
                    wakeLock.Release();
                }
            }

            return StartCommandResult.Sticky;
        }

        private void RegisterNotification()
        {
            try
            {
                NotificationChannel channel = new NotificationChannel("serviciochannel", "savegambling", NotificationImportance.Max);

                NotificationManager manager = (NotificationManager)MainActivity.ActivityCurrent.GetSystemService(Context.NotificationService);

                manager.CreateNotificationChannel(channel);

                Notification notification = new Notification.Builder(this, "serviciochannel")
                    .SetContentTitle("Servicio en segundo plano")
                    .SetOngoing(true)
                    .Build();

                StartForeground(100, notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering notification: {ex.Message}");
            }
        }

        public void StartConnection()
        {

            try
            {
                Intent startintent = new Intent(MainActivity.ActivityCurrent, typeof(SignalRService));
                startintent.SetAction("START_SERVICE");
                MainActivity.ActivityCurrent.StartService(startintent);

                InitializeSignalR();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
            }
        }

        public async Task StopConnection()
        {
            try
            {
                Intent stopintent = new Intent(MainActivity.ActivityCurrent, typeof(SignalRService));
                stopintent.SetAction("STOP_SERVICE");
                MainActivity.ActivityCurrent.StartService(stopintent);
                if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping SignalR connection: {ex.Message}");
            }
        }

        private void InitializeSignalR()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl("https://pablogproject-001-site1.jtempurl.com/ExtensionHub", (opts) =>
                {
                    opts.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                            clientHandler.ServerCertificateCustomValidationCallback +=
                                (sender, certificate, chain, sslPolicyErrors) => { return true; };
                        return message;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            hubConnection.Closed += async (error) =>
            {
                ReconnectWithBackoff();
            };

            hubConnection.On<string, string>("ReceiveMessage", async (name, message) =>
            {
                oModel = JsonConvert.DeserializeObject<ContactModel>(message);

                await cContacto.RequestWriteContactPermission(oModel);
            });
            StartHubConnection();
        }


        private async void StartHubConnection()
        {
            try
            {
                //string groupToJoin = Preferences.Get("pGroup", null);
                //string groupToJoin = "group_1708389397_MEY7xP";
                //if (!string.IsNullOrEmpty(groupToJoin))
                //{
                //    await hubConnection.InvokeAsync("JoinGroup", groupToJoin);
                //}
                await hubConnection.StartAsync();
                string groupName = "group_1708389397_MEY7xP";
                await hubConnection.InvokeAsync("JoinGroup", groupName);
                Console.WriteLine("Hub connection started successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting hub connection: {ex.Message}");
            }
        }

        private async void EnviarMensajeAlHub(string nombre, string mensaje)
        {
            try
            {
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.InvokeAsync("SendMessage", nombre, mensaje);
                    Console.WriteLine("Mensaje enviado al hub correctamente");
                }
                else
                {
                    Console.WriteLine("Intento de enviar mensaje fallido: La conexión al hub no está activa.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el mensaje al hub: {ex.Message}");
            }
        }

        private async void DetenerHubConnection()
        {
            try
            {
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.StopAsync();
                    Console.WriteLine("Hub connection stopped successfully");
                }
                else
                {
                    Console.WriteLine("Intento de detener la conexión fallido: La conexión al hub no está activa.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detener la conexión al hub: {ex.Message}");
            }
        }

        public async void ReconnectWithBackoff()
        {
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                int retries = 0;
                const int maxRetries = 5;
                const int baseDelaySeconds = 2;

                while (retries < maxRetries && hubConnection.State != HubConnectionState.Connected)
                {
                    Console.WriteLine($"Attempting to reconnect (attempt {retries + 1})");
                    await hubConnection.StartAsync();
                    await Task.Delay((int)Math.Pow(baseDelaySeconds, retries) * 1000);
                    retries++;
                }

                if (hubConnection.State != HubConnectionState.Connected)
                {
                    Console.WriteLine("Failed to reconnect after multiple attempts. Stopping connection.");
                }
                else
                {
                    Console.WriteLine("Reconnected successfully");
                }
            }
            else
            {
                Console.WriteLine("HubConnection is not in Disconnected state. Skipping reconnection attempt.");
            }
        }


        bool ISignalRServices.IsForegroundServiceRunning()
        {
            return IsForegroundServiceRunning;
        }
    }
}
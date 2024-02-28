using SaveGamblingContactXAM.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms;
using Android.Content;
using Android.Provider;
using Newtonsoft.Json;

namespace SaveGamblingContactXAM.Utils
{
    public class CreateContact
    {
        string sPreference = Preferences.Get("sUser", null);
        public void AddContact(ContactModel oContact)
        {
            List<ContentProviderOperation> ops = new List<ContentProviderOperation>();

            ContentProviderOperation.Builder builder =
            ContentProviderOperation.NewInsert(ContactsContract.RawContacts.ContentUri);
            builder.WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountType, null);
            builder.WithValue(ContactsContract.RawContacts.InterfaceConsts.AccountName, null);
            ops.Add(builder.Build());

            //Name  
            builder = ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri);
            builder.WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0);
            builder.WithValue(ContactsContract.Data.InterfaceConsts.Mimetype,
                              ContactsContract.CommonDataKinds.StructuredName.ContentItemType);
            builder.WithValue(ContactsContract.CommonDataKinds.StructuredName.DisplayName, oContact.Name);
            //builder.WithValue(ContactsContract.CommonDataKinds.StructuredName.GivenName, firstName);  
            ops.Add(builder.Build());

            //Number1  
            builder = ContentProviderOperation.NewInsert(ContactsContract.Data.ContentUri);
            builder.WithValueBackReference(ContactsContract.Data.InterfaceConsts.RawContactId, 0);
            builder.WithValue(ContactsContract.Data.InterfaceConsts.Mimetype,
                              ContactsContract.CommonDataKinds.Phone.ContentItemType);
            builder.WithValue(ContactsContract.CommonDataKinds.Phone.Number, oContact.PhoneNumber);
            builder.WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type,
                              ContactsContract.CommonDataKinds.Phone.InterfaceConsts.TypeCustom);
            builder.WithValue(ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Data2, (int)PhoneDataKind.Mobile);

            ops.Add(builder.Build());



            //Add the new contact
            ContentProviderResult[] res;
            try
            {
                res = Android.App.Application.Context.ContentResolver.ApplyBatch(ContactsContract.Authority, ops);
                ops.Clear();//Add this line  
                CreateRegisterContact(oContact);
            }
            catch (System.Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }


        // write contact
        public async Task RequestWriteContactPermission(ContactModel oModel)
        {
            bool bExist = await GetContactList(oModel.PhoneNumber);
            if (!bExist)
            {
                var status = PermissionStatus.Unknown;
                {
                    status = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();

                    if (status == PermissionStatus.Granted)
                    {

                        AddContact(oModel);//passing name, phone and email
                        return;
                    }

                    if (Permissions.ShouldShowRationale<Permissions.Phone>())
                    {
                        await Shell.Current.DisplayAlert("Needs permissions", "BECAUSE!!!", "OK");
                    }

                    status = await Permissions.RequestAsync<Permissions.ContactsWrite>();
                }

                if (status != PermissionStatus.Granted)
                    await Shell.Current.DisplayAlert("Permission required",
                        "Write Contact permission is required for test. " +
                        "We just want to do a test.", "OK");

                else if (status == PermissionStatus.Granted)
                {
                    AddContact(oModel);//passing name, phone and email
                }
            }
        }


        public async Task<PermissionStatus> CheckAndRequestContactsWritePermission(ContactModel oModel)
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.ContactsWrite>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.ContactsWrite>())
            {
                await Shell.Current.DisplayAlert("Need permission", "We need the Write Contact permission", "Ok");
            }

            status = await Permissions.RequestAsync<Permissions.ContactsWrite>();

            if (status == PermissionStatus.Granted)
            {
                AddContact(oModel);
            }
            else
            {
                await Shell.Current.DisplayAlert("Permission required",
                      "Write Contact permission is required for test . " +
                      "We just want to test", "OK");

            }

            return status;
        }

        public async void CreateRegisterContact(ContactModel oModel)
        {
            string sUrlApi = @"https://pablogproject-001-site1.jtempurl.com/api/Contact/CreateContact";
            using (HttpClient oClient = new HttpClient())
            {

                oModel.IsRegistered = true;
                string sJson = JsonConvert.SerializeObject(oModel);
                var oContent = new StringContent(sJson, Encoding.UTF8, "application/json");
                HttpResponseMessage oResponse = await oClient.PostAsync(sUrlApi, oContent);
            }
        }

        private async Task<bool> GetContactList(string iNumero)
        {
            string sUrlApi = @"https://pablogproject-001-site1.jtempurl.com/api/Contact/GetContacts";
            using (HttpClient oClient = new HttpClient())
            {
                HttpResponseMessage oResponse = await oClient.GetAsync(sUrlApi);
                oResponse.EnsureSuccessStatusCode();

                string sReplyGet = await oResponse.Content.ReadAsStringAsync();

                List<ContactModel> lstContact = JsonConvert.DeserializeObject<List<ContactModel>>(sReplyGet);

                List<ContactModel> lstContactAdd = new List<ContactModel>();

                var contactoEncontrado = lstContact.Find(c => c.PhoneNumber == iNumero && c.UserLine == sPreference);

                return contactoEncontrado != null;
            }
        }
    }
}

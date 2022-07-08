using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using BestHTTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MailScreenUIController : MonoBehaviour
{
    // This defines the mail JSON object we send to the server.
    public class SentMail
    {
        public string title;

        public string longText;

        public string forPromoCode;

        public Attachment[] attachedCharacters;

        public Attachment[] attachedWeapons;

        public Attachment[] attachedAccessories;

        public Attachment[] attachedItems;

        public SentMail(string title, string content, string promoCode = "")
        {
            this.title = title;
            this.longText = content;
            this.forPromoCode = promoCode;

            this.attachedCharacters = new Attachment[0];
            this.attachedWeapons = new Attachment[0];
            this.attachedAccessories = new Attachment[0];
            this.attachedItems = new Attachment[0];
        }
    }

    // All the mail information received from the server.
    public class MailList
    {
        public Mail[] mails;
    }

    // The json parser needs this to parse responses where we expect only one mail object.
    public class SingleMail
    {
        public Mail mail;
    }

    // A single mail object parsed from the server.
    public class Mail
    {
        public string id;

        public string title;

        public string longText;

        public string forPromoCode;

        public string sentAt;

        public string readAt;

        public string claimedAt;

        public string recipient;

        public Attachment[] attachedCharacters;

        public Attachment[] attachedWeapons;

        public Attachment[] attachedAccessories;

        public Attachment[] attachedItems;
    }

    // A single mail attachment (characters, weapons, etc.).
    public class Attachment
    {
        public string contentId;

        public int quantity;

        public Attachment(string id, int quantity)
        {
            this.contentId = id;
            this.quantity = quantity;
        }
    }

    public FirebaseHandler firebase;

    // The mail ui item prefab to be copied.
    public GameObject mailPrefab;

    // The most recent mail information from the server.
    public MailList currentMailList;

    // The mail currently selected in the ui.
    public MailData currentSelectedMail;

    // Mail information referenced by the mail's id attribute.
    public Dictionary<string, Mail> mailsById = new Dictionary<string, Mail>();

    // Mail ui elements referenced by the mail's id attribute.
    public Dictionary<string, MailData> mailUiItemsById = new Dictionary<string, MailData>();

    // The parent content gameobject for the mail list.
    public GameObject mailListContainer;

    public TMP_Text titleDisplayText;

    public TMP_Text sendDateDisplayText;

    public TMP_Text contentDisplayText;

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        RequestUpdatedMailList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Sends a me/mail request to the server to get the latest mails. Updating the UI is handled in a callback method.
    public void RequestUpdatedMailList()
    {
        // Send a /me/mail request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me/mail"), HTTPMethods.Get, OnMeMailRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    public void OnRefreshButtonClicked()
    {

    }

    public void OnClaimButtonClicked()
    {
        // Send a /mail/{player}/{id}/claim request to the server.
        // TODO: Once the account id is properly stored, use that here as the player argument.
        string requestUrl = string.Format("http://testserver.magic-connect.com/mail/{0}/{1}/claim", "62a3e9553a23910038e9f4bc", currentSelectedMail.mailData.id);
        Debug.LogFormat("Claim Request URL: {0}", requestUrl);
        HTTPRequest request = new HTTPRequest(new Uri(requestUrl), HTTPMethods.Post, OnMailClaimedRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        //request.Send();
    }

    public void OnClaimAllButtonClicked()
    {

    }

    public void OnDeleteAllButtonClicked()
    {}

    public void OnDeleteButtonClicked()
    {}

    // Called when a mail item has been selected/clicked in the ui.
    public void OnMailSelected(string id)
    {
        // Update the info panel with the mail's contents.
        Mail mailInfo = mailsById[id];

        titleDisplayText.text = mailInfo.title;
        sendDateDisplayText.text = mailInfo.sentAt;
        contentDisplayText.text = mailInfo.longText;

        // Tell the old mail item it is no longer selected, then track the newly selected mail item.
        if(currentSelectedMail)
        {
            currentSelectedMail.Unselect();
        }
        
        currentSelectedMail = mailUiItemsById[id];

        // If the mail was previously unread, tell the server that it has been read.
        if(mailInfo.readAt == null && mailInfo.readAt != "")
        {
            // Send a /mail/{player}/{id}/read request to the server.
            // TODO: Once the account id is properly stored, use that here as the player argument.
            string requestUrl = string.Format("http://testserver.magic-connect.com/mail/{0}/{1}/read", "62a3e9553a23910038e9f4bc", currentSelectedMail.mailData.id);
            Debug.LogFormat("Read Request URL: {0}", requestUrl);
            HTTPRequest request = new HTTPRequest(new Uri(requestUrl), HTTPMethods.Post, OnMailReadRequestFinished);

            request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

            request.Send();
        }
    }

    // Debug methods for sending mails to myself. Useful for testing but not much else.
    // Note: If we support sending mails to other users this code would be useful for copy/pasting.
    #region Test Mail Methods
    public void SendTestMail()
    {
        // Create a /mail/{player} post request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/mail/62a3e9553a23910038e9f4bc"), HTTPMethods.Post, OnMeMailRequestFinished);

        // Send authorization token with the request.
        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        SentMail newMail = new SentMail("Test Mail", "This is a test mail to myself.");
        Attachment character1 = new Attachment("fe162e55-5166-4bfd-81c9-171e66f80174", 1); // Lana Elenore
        Attachment character2 = new Attachment("5d966789-8e0d-44dc-aa2c-2b48a7377d83", 1); // Fuyumi Hana
        newMail.attachedCharacters = new Attachment[]{character1, character2};

        Attachment accessory1 = new Attachment("35a0030c-63d6-45dc-93bd-cfc96fe36d7a", 1); // bauble
        Attachment accessory2 = new Attachment("558084ce-4209-4496-9f4c-81399b518b7c", 1); // magic emblem
        newMail.attachedAccessories = new Attachment[]{accessory1, accessory2};

        Attachment weapon1 = new Attachment("538403a9-5a15-411a-8ee3-5037b1f9a0b2", 1); // wood axe
        Attachment weapon2 = new Attachment("b778ddc7-1dcb-481b-858e-b3b3cd97b9ae", 1); // sharp axe
        newMail.attachedWeapons = new Attachment[]{weapon1, weapon2};

        Attachment item1 = new Attachment("08b85b71-a0ec-4202-88cc-2af7b5940f65", 1000); // gold
        Attachment item2 = new Attachment("bee2638a-0f01-4aeb-a2db-082fb708dd7c", 1); // small scrap
        newMail.attachedItems = new Attachment[]{item1, item2};

        var data = JObject.FromObject(newMail);

        // Add JSON data to the request.
        request.SetHeader("Content-Type", "application/json; charset=UTF-8");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(data.ToString());

        Debug.Log(data.ToString(), this);
        
        // Send the request to the server.
        request.Send();
    }

    public void SendTestMailTypeTwo()
    {
        // Create a /mail/{player} post request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/mail/62a3e9553a23910038e9f4bc"), HTTPMethods.Post, OnMeMailRequestFinished);

        // Send authorization token with the request.
        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        SentMail newMail = new SentMail("Test Mail 2", "This is another test mail to myself.");

        Attachment weapon1 = new Attachment("538403a9-5a15-411a-8ee3-5037b1f9a0b2", 1); // wood axe
        Attachment weapon2 = new Attachment("b778ddc7-1dcb-481b-858e-b3b3cd97b9ae", 1); // sharp axe
        newMail.attachedWeapons = new Attachment[]{weapon1, weapon2};

        var data = JObject.FromObject(newMail);

        // Add JSON data to the request.
        request.SetHeader("Content-Type", "application/json; charset=UTF-8");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(data.ToString());

        Debug.Log(data.ToString(), this);
        
        // Send the request to the server.
        request.Send();
    }

    public void SendTestMailTypeThree()
    {
        // Create a /mail/{player} post request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/mail/62a3e9553a23910038e9f4bc"), HTTPMethods.Post, OnMeMailRequestFinished);

        // Send authorization token with the request.
        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        SentMail newMail = new SentMail("Test Mail 3", "This is yet another test mail to myself.");

        var data = JObject.FromObject(newMail);

        // Add JSON data to the request.
        request.SetHeader("Content-Type", "application/json; charset=UTF-8");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(data.ToString());

        Debug.Log(data.ToString(), this);
        
        // Send the request to the server.
        request.Send();
    }

    public void OnTestSendMailRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    // Dump all headers and their values into the console.
                    Debug.LogFormat(this, "API Response Headers/Values:");
                    foreach(string header in response.Headers.Keys)
                    {
                        Debug.LogFormat(this, "Header: {0} Value(s): {1}", header, response.Headers[header]);
                    }

                    Debug.LogFormat(this, "Response Data: {0}", response.DataAsText);
                    Debug.LogFormat(this, "Status Code: {0} Message: {1}", response.StatusCode, response.Message);
                    Debug.LogFormat(this, "Major Version: {0} Minor Version: {1}", response.VersionMajor, response.VersionMinor);
                    Debug.LogFormat(this, "Was from cache: {0}", response.IsFromCache);

                    //ParseResponseIntoUIInfo(response.DataAsText);
                }
                else
                {
                    Debug.LogWarningFormat(this, "Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", response.StatusCode, response.Message, response.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Mail Screen: Request finished with an error: " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"), this);
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Mail Screen: Request aborted.", this);
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Mail Screen: Connection timed out.", this);
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Mail Screen: Processing the request timed out.", this);
                break;
        }// end switch block
    }
    #endregion

    public void ParseMailResponseIntoObjects(string data)
    {
        MailList mailList = JsonConvert.DeserializeObject<MailList>(data);

        if(mailList != null && mailList.mails != null)
        {
            foreach(Mail mail in mailList.mails)
            {
                Debug.LogFormat(this, "Mail Id: {0}", mail.id);

                // Create a new mail instance from the prefab and add it to the mail list ui.
                GameObject newMail = Instantiate(mailPrefab, mailListContainer.transform);
                MailData newMailData = newMail.GetComponent<MailData>();

                // Pass it the parsed mail information.
                newMailData.mailData = mail;

                // Subscribe to the mail item's click event.
                newMailData.onMailSelected += OnMailSelected;

                // Store the mail information and ui items so they can be easily referenced when events occur.
                mailsById.Add(mail.id, mail);
                mailUiItemsById.Add(newMailData.mailData.id, newMailData);
            }
        }
        else
        {
            // TODO: This isn't necessarily an error. It could just mean there's no mail to display.
            Debug.LogErrorFormat(this, "Mail UI Controller: Something went wrong parsing the /me/mail response data -> Data: {0}", data);
        }
    }

    public void OnMeMailRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    // Dump all headers and their values into the console.
                    Debug.LogFormat(this, "API Response Headers/Values:");
                    foreach(string header in response.Headers.Keys)
                    {
                        Debug.LogFormat(this, "Header: {0} Value(s): {1}", header, response.Headers[header]);
                    }

                    Debug.LogFormat(this, "Response Data: {0}", response.DataAsText);
                    Debug.LogFormat(this, "Status Code: {0} Message: {1}", response.StatusCode, response.Message);
                    Debug.LogFormat(this, "Major Version: {0} Minor Version: {1}", response.VersionMajor, response.VersionMinor);
                    Debug.LogFormat(this, "Was from cache: {0}", response.IsFromCache);

                    ParseMailResponseIntoObjects(response.DataAsText);
                }
                else
                {
                    Debug.LogWarningFormat(this, "Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", response.StatusCode, response.Message, response.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Mail Screen: Request finished with an error: " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"), this);
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Mail Screen: Request aborted.", this);
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Mail Screen: Connection timed out.", this);
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Mail Screen: Processing the request timed out.", this);
                break;
        }// end switch block
    }

    public void OnMailReadRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    // Dump all headers and their values into the console.
                    Debug.LogFormat(this, "API Response Headers/Values:");
                    foreach(string header in response.Headers.Keys)
                    {
                        Debug.LogFormat(this, "Header: {0} Value(s): {1}", header, response.Headers[header]);
                    }

                    Debug.LogFormat(this, "Response Data: {0}", response.DataAsText);
                    Debug.LogFormat(this, "Status Code: {0} Message: {1}", response.StatusCode, response.Message);
                    Debug.LogFormat(this, "Major Version: {0} Minor Version: {1}", response.VersionMajor, response.VersionMinor);
                    Debug.LogFormat(this, "Was from cache: {0}", response.IsFromCache);

                    //ParseMailResponseIntoObjects(response.DataAsText);

                    Mail updatedMail = JsonConvert.DeserializeObject<SingleMail>(response.DataAsText).mail;

                    if(updatedMail != null)
                    {
                        Debug.LogFormat(this, "Updated mail information received. Refreshing the ui. -> {0}", response.DataAsText);
                        Debug.LogFormat(this, "Mail Id: {0}", updatedMail.id);
                        mailsById.Remove(updatedMail.id);
                        mailsById.Add(updatedMail.id, updatedMail);

                        mailUiItemsById[updatedMail.id].mailData = updatedMail;
                    }
                    else
                    {
                        Debug.LogErrorFormat(this, "The mail read request was successful but there was a problem parsing the updated mail information.");
                    }
                }
                else
                {
                    Debug.LogWarningFormat(this, "Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", response.StatusCode, response.Message, response.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Mail Screen: Request finished with an error: " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"), this);
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Mail Screen: Request aborted.", this);
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Mail Screen: Connection timed out.", this);
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Mail Screen: Processing the request timed out.", this);
                break;
        }// end switch block
    }

    public void OnMailClaimedRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    // Dump all headers and their values into the console.
                    Debug.LogFormat(this, "API Response Headers/Values:");
                    foreach(string header in response.Headers.Keys)
                    {
                        Debug.LogFormat(this, "Header: {0} Value(s): {1}", header, response.Headers[header]);
                    }

                    Debug.LogFormat(this, "Response Data: {0}", response.DataAsText);
                    Debug.LogFormat(this, "Status Code: {0} Message: {1}", response.StatusCode, response.Message);
                    Debug.LogFormat(this, "Major Version: {0} Minor Version: {1}", response.VersionMajor, response.VersionMinor);
                    Debug.LogFormat(this, "Was from cache: {0}", response.IsFromCache);

                    //ParseMailResponseIntoObjects(response.DataAsText);
                }
                else
                {
                    Debug.LogWarningFormat(this, "Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", response.StatusCode, response.Message, response.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Mail Screen: Request finished with an error: " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"), this);
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Mail Screen: Request aborted.", this);
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Mail Screen: Connection timed out.", this);
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Mail Screen: Processing the request timed out.", this);
                break;
        }// end switch block
    }
}

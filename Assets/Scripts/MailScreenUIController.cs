using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using BestHTTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MailScreenUIController : MonoBehaviour
{
    public FirebaseHandler firebase;

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

    // An individual character entry sent with the mail.
    public class Character
    {
        public string ContentId;

        public int Quantity;
    }

    // An individual weapon entry sent with the mail.
    public class Weapon
    {
        public string ContentId;

        public int Quantity;
    }

    // An individual accessory entry sent with the mail.
    public class Accessory
    {
        public string ContentId;

        public int Quantity;
    }

    // An individual item entry sent with the mail.
    public class Item
    {
        public string ContentId;

        public int Quantity;
    }

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        // Send a /me/mail request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me/mail"), OnMeMailRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendTestMail()
    {
        // Create a /mail/{player} post request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/mail/62a3e9553a23910038e9f4bc"), HTTPMethods.Post, OnMeMailRequestFinished);

        //request.FormUsage = BestHTTP.Forms.HTTPFormUsage.Multipart;

        // Send authorization token with the request.
        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        // Build JSON data to be sent to the server.
        /*
        var data = new JObject();
        data.Add("title", "Test Mail");
        data.Add("longText", "This is a test mail to myself.");
        data.Add("forPromoCode", "");
        
        // Character attachment data:
        var characters = new JObject();
        characters.Add("contentId", "");
        characters.Add("quantity", 0);

        // Weapon attachment data:
        var weapons = new JObject();
        weapons.Add("contentId", "");
        weapons.Add("quantity", 0);

        // Accessory attachment data:
        var accessories = new JObject();
        accessories.Add("contentId", "");
        accessories.Add("quantity", 0);

        // Item attachment data:
        var item1 = new JObject();
        item1.Add("contentId", "");
        item1.Add("quantity", 0);

        var item2 = new JObject();
        item2.Add("contentId", "");
        item2.Add("quantity", 0);

        var items = new JToken[2];
        items[0] = item1;
        items[1] = item2;

        // Add attachment data to core JSON data:
        data.Add("attachedCharacters", characters);
        data.Add("attachedWeapons", items);
        data.Add("attachedAccessories", accessories);
        data.Add("attachedItems", items);
        */

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
        //request.SetHeader("Content-Type", "application/json; charset=UTF-8");
        request.RawData = System.Text.Encoding.UTF8.GetBytes(data.ToString());

        Debug.Log(data.ToString(), this);

        //string attributes = string.Format("\"title\": {0}, \"longText\": {1}, \"forPromoCode\": {2}", "Test Mail", "This is a test mail to myself.", "");
        
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
}

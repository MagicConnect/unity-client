using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MailData : MonoBehaviour
{
    public string id = "";

    public string title = "";

    public string longText = "";

    // TODO: Maybe use a date/time object?
    public string sendDate = "";
    
    public string readDate = "";

    public string claimDate = "";

    // The mail's attachments, indicated by content ids.
    public string[] attachedCharacters;

    public string[] attachedWeapons;

    public string[] attachedAccessories;

    public string[] attachedItems;

    public string recipient = "";

    public TMP_Text titleDisplayText;

    public TMP_Text sendDateDisplayText;

    public GameObject attachmentIcon;

    public Color defaultColor;

    public Color selectedColor;

    public Image backgroundPanel;

    public bool isSelected = false;

    // Using this is probably more convenient than having the above fields.
    private MailScreenUIController.Mail _mailData;
    public MailScreenUIController.Mail mailData{
        get => _mailData;
        set
        {
            _mailData = value;

            UpdateUi();
        }
    }

    // Invoked when the click event is called on this object. Lets listeners know which mail was selected.
    public event Action<string> onMailSelected;

    void Awake()
    {
        defaultColor = backgroundPanel.color;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Tells this mail item it is no longer selected, so it should update itself accordingly.
    public void Unselect()
    {
        isSelected = false;

        UpdateUi();
    }

    // Updates this mail's ui elements to reflect any changes in data.
    public void UpdateUi()
    {
        // Update basic information display.
        titleDisplayText.text = mailData.title;
        sendDateDisplayText.text = mailData.sentAt;

        // TODO: Check if mail has been read, and figure out a visual to represent the mail's read status.

        // Check if the mail item is selected in the ui, and change the color to reflect it.
        if(isSelected)
        {
            backgroundPanel.color = selectedColor;
        }
        else
        {
            backgroundPanel.color = defaultColor;
        }

        // Check if the mail has any attachments.
        bool hasAccessoryAttachments = mailData.attachedAccessories != null && mailData.attachedAccessories.Length > 0;
        bool hasWeaponAttachments = mailData.attachedWeapons != null && mailData.attachedWeapons.Length > 0;
        bool hasCharacterAttachments = mailData.attachedCharacters != null && mailData.attachedCharacters.Length > 0;
        bool hasItemAttachments = mailData.attachedItems != null && mailData.attachedItems.Length > 0;

        // TODO: Check if attachments have been claimed.

        // If there are any unclaimed attachments, show the attachment icon.
        if(hasAccessoryAttachments || hasWeaponAttachments || hasCharacterAttachments || hasItemAttachments)
        {
            attachmentIcon.SetActive(true);
        }
    }

    public void OnMailClicked()
    {
        isSelected = true;

        // Update the UI.
        UpdateUi();

        // Fire off the selected event.
        if(onMailSelected != null)
        {
            onMailSelected.Invoke(mailData.id);
        }
    }
}

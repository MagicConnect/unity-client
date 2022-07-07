using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mail : MonoBehaviour
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

    // Invoked when the click event is called on this object. Lets listeners know which mail was selected.
    public event Action<string> onMailSelected;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Updates this mail's ui elements to reflect any changes in data.
    public void UpdateUi()
    {}

    public void OnMailClicked()
    {
        // Perform any UI animations.

        // Fire off the selected event.
        if(onMailSelected != null)
        {
            onMailSelected.Invoke(id);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCard : MonoBehaviour
{
    // The character headshot/bodyshot.
    public Image characterArt;

    // The attack type of the character, used for filtering and showing the right type symbol.
    public Image characterType;

    // The character's full name.
    public TMP_Text characterName;

    // The character's current level.
    public TMP_Text characterLevel;

    private string _contentId;
    public string contentId
    {
        get => _contentId;
        set
        {
            _contentId = value;
            RefreshUi();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Refreshes the character card ui elements to reflect any changes in data.
    public void RefreshUi()
    {
        // Use the current content id to find the character information in the cached game data.
    }
}

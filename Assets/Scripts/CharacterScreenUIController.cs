using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScreenUIController : MonoBehaviour
{
    // When a character is selected this screen is shown and displays important information about that character.
    public GameObject characterDetailsScreen;

    // The full list of characters as shown by a series of character 'cards'.
    public GameObject characterListScreen;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Subscribes to the given character card's event(s) so this screen can display the character details screen.
    public void SubscribeToCardEvents(CharacterCard card)
    {
        card.OnCharacterCardClicked += OnCharacterCardClicked;
    }

    // When the grid button is clicked the full character list is shown again.
    public void OnSidebarGridButtonClicked()
    {
        ShowCharacterList();
        HideCharacterDetails();
    }

    // When a character's sidebar icon is clicked, this method is called.
    public void OnSidebarCharacterClicked(string contentId)
    {
        ShowCharacterDetails(contentId);
        HideCharacterList();
    }

    // When a character card in the character list screen is selected, this method is called.
    public void OnCharacterCardClicked(string id)
    {
        ShowCharacterDetails(id);
        HideCharacterList();
    }

    // Makes the character list screen active so it can be seen again.
    public void ShowCharacterList()
    {
        characterListScreen.SetActive(true);
    }

    // Hides the character list screen so the character details screen can be displayed.
    public void HideCharacterList()
    {
        characterListScreen.SetActive(false);
    }

    // Shows the character details screen and passes along the character's content id so the details screen knows
    // what to display.
    public void ShowCharacterDetails(string characterId)
    {
        characterDetailsScreen.SetActive(true);
        characterDetailsScreen.GetComponent<CharacterDetailsUIController>().LoadCharacterDetails(characterId);
    }

    // Makes the character details screen invisible so that the character list can be displayed again.
    public void HideCharacterDetails()
    {
        characterDetailsScreen.SetActive(false);
    }
}

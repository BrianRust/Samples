using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityDebugger;

public class HandManager : MonoBehaviour {

	public PlotCard cardInstance;

	private List<PlotCard> CardsInHand;
	private int NumCards = 0;
	
	// Use this for initialization
	void Start() {
		CardsInHand = new List<PlotCard>();
	}
	
	// Update is called once per frame
	void Update() {
		if (GameplayManager.SharedInstance.Game.LocalPlayer != null) {
			if (CheckForHandUpdate()) {
				RefreshHand();
			}

			foreach (PlotCard playerCard in CardsInHand) {
				playerCard.UpdateCardHighlight();
			}
		}
	}

	void RefreshHand() {
		NumCards = GameplayManager.SharedInstance.Game.LocalPlayer.NumCards;

		foreach (PlotCard card in CardsInHand) {
			Destroy (card.gameObject);
		}

		CardsInHand.Clear();

		int counter = GameplayManager.SharedInstance.Game.LocalPlayer.Cards.Count - 1;

		float zOffset = 10.0f;

		foreach (CardModel card in GameplayManager.SharedInstance.Game.LocalPlayer.Cards) {
			PlotCard newCard = Instantiate(cardInstance, new Vector3(525.1f + ((float)counter * 50.0f), -20.5f, zOffset), Quaternion.identity) as PlotCard;

			newCard.SetCardModel(card);
			newCard.transform.SetParent(this.transform);
			newCard.transform.localScale = new Vector3(newCard.transform.localScale.x, newCard.transform.localScale.y, 1.0f);
			newCard.transform.SetAsLastSibling();

			counter--;
			CardsInHand.Add(newCard);

			if (!newCard.AssetModel.Hidden) {
				int playerID = GameplayManager.SharedInstance.Game.LocalPlayer.Id;
				
				Image newImage;
				
				newImage = Instantiate(GameObject.Find("GameManager").GetComponent<GameManager>().playersByID[playerID].avatar, new Vector3 (newCard.transform.FindChild("Image").transform.position.x, newCard.transform.FindChild("Image").transform.position.y, zOffset), Quaternion.identity) as Image;
				newImage.transform.SetParent (newCard.transform.FindChild("Image").transform);
				newImage.transform.rotation = newCard.transform.FindChild("Image").transform.rotation;
				newImage.transform.localPosition = new Vector3 (0.0f, 9.75f, newImage.transform.localPosition.z);
				newImage.transform.localScale = new Vector3 (16.0f, 16.0f, 1.0f);
				
				Text newText;
				newText = Instantiate(GameObject.Find("PlayerName").GetComponent<Text>(), new Vector3 (newCard.transform.FindChild("Image").transform.position.x, newCard.transform.FindChild("Image").transform.position.y, zOffset), Quaternion.identity) as Text;
				newText.transform.SetParent (newCard.transform.FindChild("Image").transform);
				newText.text = GameObject.Find("GameManager").GetComponent<GameManager>().playersByID[playerID].name;
				newText.alignment = TextAnchor.MiddleLeft;
				newText.transform.rotation = newCard.transform.FindChild("Image").transform.rotation;
				newText.transform.localPosition = new Vector3 (-9.0f, 47.0f, newText.transform.localPosition.z + 1.0f);
				newText.transform.localScale = new Vector3 (0.4f, 0.4f, 1.0f);
			} else {
				//show question mark
				Image unknownImage = newCard.transform.FindChild("Image").FindChild("UnknownAvatar").GetComponent<Image>();
				Color c = unknownImage.color;
				c.a = 1.0f;
				unknownImage.color = c;
			}

			zOffset -= 1.2f;
		}
	}

	bool CheckForHandUpdate() {
		if (NumCards != GameplayManager.SharedInstance.Game.LocalPlayer.NumCards) {
			return true;
		} else {
			bool CardFound = false;
			foreach (CardModel card in GameplayManager.SharedInstance.Game.LocalPlayer.Cards) {
				CardFound = false;
				foreach (PlotCard playerCard in CardsInHand) {
					if (card.Id == playerCard.Model.Id) {
						CardFound = true;
						break;
					}
				}

				if (!CardFound) {
					return true;
				}
			}
		}

		return false;
	}
}

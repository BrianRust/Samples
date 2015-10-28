using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityDebugger;

public class QueueManager : MonoBehaviour {

	public PlotCard cardInstance;
	
	private List<PlotCard> CardsInQueue;
	private int NumCards = 0;

	// Use this for initialization
	void Start() {
		CardsInQueue = new List<PlotCard>();
	}
	
	// Update is called once per frame
	void Update() {
		if (GameplayManager.SharedInstance.Game.CardState != null) {
			if (CheckForQueueUpdate()) {
				RefreshQueue();
			}
		}
	}

	bool CheckForQueueUpdate() {
		if (NumCards != GameplayManager.SharedInstance.Game.CardState.Cards.Count) {
			return true;
		} else {
			bool CardFound = false;
			foreach (CardModel card in GameplayManager.SharedInstance.Game.CardState.Cards) {
				CardFound = false;
				foreach (PlotCard queueCard in CardsInQueue) {
					if (queueCard.Model != null) {
						if (card.Id == queueCard.Model.Id) {
							CardFound = true;
							break;
						}
					}
					else {

					}
				}
				
				if (!CardFound) {
					return true;
				}
			}
		}
		
		return false;
	}

	void RefreshQueue() {
		NumCards = GameplayManager.SharedInstance.Game.CardState.Cards.Count;

		foreach (PlotCard card in CardsInQueue) {
			Destroy (card.gameObject);
		}

		CardsInQueue.Clear();

		float offset = 0.0f;
		float startLocation = 115.1f;
		float nextCardLocation = 165.1f;
		
		for (int index = GameplayManager.SharedInstance.Game.CardState.Cards.Count - 1; index >= 0; index--) {
			PlotCard newCard;
			if (index == 0) {
				if (GameplayManager.SharedInstance.Game.CardState.Cards.Count > 1) {
					newCard = Instantiate(cardInstance, new Vector3(nextCardLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as PlotCard;
				} else {
					newCard = Instantiate(cardInstance, new Vector3(startLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as PlotCard;
				}
			} else {
				newCard = Instantiate(cardInstance, new Vector3(startLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as PlotCard;
			}

			newCard.transform.localScale = new Vector3(7.0f, 7.0f, 1.0f);
			newCard.SetCardModel(GameplayManager.SharedInstance.Game.CardState.Cards[index]);
			newCard.transform.SetParent(this.transform);
			newCard.touchEnabled = false;
			newCard.faceDown = true;
			newCard.FlipImage();

			if (!newCard.AssetModel.Hidden) {
				if (index == 1) {
					int playerID = GameplayManager.SharedInstance.Game.CardState.Cards[index].From;
					Image newImage = Instantiate(GameObject.Find("GameManager").GetComponent<GameManager>().playersByID[playerID].avatar, new Vector3(startLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as Image;
					newImage.transform.localScale = new Vector3(20.0f, 20.0f, 1.0f);
					newImage.transform.SetParent(newCard.transform);
				} else if (index == 0) {
					int playerID = GameplayManager.SharedInstance.Game.CardState.Cards[index].From;
					Image newImage;
				
					if (GameplayManager.SharedInstance.Game.CardState.Cards.Count > 1) {
						newImage = Instantiate(GameObject.Find("GameManager").GetComponent<GameManager>().playersByID[playerID].avatar, new Vector3(nextCardLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as Image;
					} else {
						newImage = Instantiate(GameObject.Find("GameManager").GetComponent<GameManager>().playersByID[playerID].avatar, new Vector3(startLocation + (offset * 20.0f), 430.5f), Quaternion.identity) as Image;
					}
				
					newImage.transform.localScale = new Vector3(20.0f, 20.0f, 1.0f);
					newImage.transform.SetParent(newCard.transform);
				}
			}

			offset++;
			CardsInQueue.Add(newCard);
		}
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityDebugger;

public class PlotCard : MonoBehaviour {

	public CardModel Model { get; private set; }
	public CardAssetModel AssetModel { get; private set; }

	public bool faceDown;
	public bool touchEnabled;

	private float snapDistance;
	private Vector3 startPosition;
	private Vector3 startScale;
	private int startLayer;

	private Player[] players;
	private float lastTouchDownTime;
	private bool zoomed;
	
	private MeshRenderer cardMesh;
	private Material replacementMat;
	
	private GameObject[] playerCardSlots;
	private GameObject generalCardSlot;
	private GameManager gameManager;
	
	private Text title;
	private Text description;
	private Text tooltip;
	private Text cost;
	
	public bool moving;
	public bool viewingCard;
	public Vector3 targetPos;
	private Vector3 velocity;

	private Vector3 zoomedPosition;
	private Vector3 zoomedScale;

	private Vector3 mouseDownStartPosition;
	private float moveThreshold;

	private float heldThreshold;
	
	//For testing
	private Vector3 cardPosition;

	private bool isMoving;
	
	// Use this for initialization
	void Start () {
		moveThreshold = 1000.0f;
		heldThreshold = 0.5f;

		snapDistance = 2500.0f;
		startPosition = transform.localPosition;
		startScale = transform.localScale;

		isMoving = false;

		zoomedScale = new Vector3(20.0f, 20.0f, 1.0f);
		zoomedPosition = new Vector3(transform.position.x, 210.0f, 0.5f);

		startLayer = GetComponent<Canvas>().sortingOrder;
		int numPlayers = GameplayManager.SharedInstance.Game.GameConfig.NumPlayers;

		players = new Player[numPlayers];
		for (int index = 0; index < numPlayers; index++) {
			players[index] = GameObject.Find("Players-" + numPlayers + "/Player " + index).GetComponent<Player>();
		}

		playerCardSlots = new GameObject[numPlayers];

		for (int index = 0; index < numPlayers; index++) {
			playerCardSlots[index] = GameObject.Find("Players-" + numPlayers + "/Player " + index + "/Player Highlight");
		}

		generalCardSlot = GameObject.Find("GeneralCardSlot");
		generalCardSlot.transform.localScale = new Vector3(40.0f, 24.0f, 1.0f);
		
		//image = transform.FindChild("Image").GetComponent<Image>();
		Transform[] children = transform.FindChild ("Image").GetComponentsInChildren<Transform>();
		
		foreach(Transform child in children)
		{
			if(child.name == "Title")
			{
				title = child.gameObject.GetComponent<Text>();
			}	
			if(child.name == "Description")
			{
				description = child.gameObject.GetComponent<Text>();
			}
			if(child.name == "Phase")
			{
				tooltip = child.gameObject.GetComponent<Text>();
			}
			if(child.name == "Cost")
			{
				cost = child.gameObject.GetComponent<Text>();
			}
		}

		HightLightDistributedCards();
	}
	
	// Update is called once per frame
	void Update () {
		if (moving) {
			transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.3f);
		}
		
		if (faceDown) {
			title.enabled = false;
			description.enabled = false;
			cost.enabled = false;
		}
		else {
			title.enabled = true;
			description.enabled = true;
			cost.enabled = true;
		}
	}

	// quick & dirty list intersection
	private static List<int> Intersect(List<int> p1, List<int> p2) {
		List<int> result = new List<int>( ( p1.Count + p2.Count ) / 2 );
		foreach (int val in p1) {
			if (p2.IndexOf(val) != -1) {
				result.Add(val);
			}
		}
		return result;
	}

	List<int> GetPotentialTargets(CardTarget targetType, int from, int inheritedTarget) {
		List<int> potentialTargets = new List<int>( players.Length );
		switch ( targetType ) {
			case CardTarget.CARD_TARGET_NONE: {
				potentialTargets.Add( -1 );
				break;
			}
			case CardTarget.CARD_TARGET_INHERIT: {
				potentialTargets.Add( inheritedTarget );
				break;
			}
			case CardTarget.CARD_TARGET_SELF: {
				potentialTargets.Add( from );
				break;
			}
			case CardTarget.CARD_TARGET_ANY_PLAYER: {
				foreach (Player p in players) {
					potentialTargets.Add(p.playerID);
				}
				break;
			}
			case CardTarget.CARD_TARGET_OTHER_PLAYER: {
				foreach (Player p in players) {
					if (p.playerID != GameplayManager.SharedInstance.Game.LocalPlayer.Id) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_ON_MISSION: {
				List<int> playersOnTeam = GameplayManager.SharedInstance.Game.GameState.CurrentMission.Team;
				foreach (Player p in players) {
					if (playersOnTeam.Contains(p.playerID)) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITH_CARDS: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].NumCards > 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITHOUT_CARDS: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].NumCards == 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITH_CONFIDENCE_TOKENS: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].NumConfidenceTokens > 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITHOUT_CONFIDENCE_TOKENS: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].NumConfidenceTokens == 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITH_POSITIVE_CONFIDENCE: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].ConfidencePositive > 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITH_NEGATIVE_CONFIDENCE: {
				foreach (Player p in players) {
					if (GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].ConfidenceNegative > 0) {
						potentialTargets.Add(p.playerID);
					}
				}
				break;
			}
			case CardTarget.CARD_TARGET_WITH_CONFIDENCE_FROM_ME: {
				foreach (Player p in players) {
					foreach (ConfidenceEventModel cem in GameplayManager.SharedInstance.Game.GameState.Players[p.playerID].ConfidenceEvents) {
						if (cem.From == GameplayManager.SharedInstance.Game.LocalPlayer.Id) {
							potentialTargets.Add(p.playerID);
							break;
						}
					}
				}
				break;
			}
		}
		return potentialTargets;
	}

	List<int> FindTargetsFromMask(int targetMask, int from, int inheritedTarget) {
		List<int> potentialTargets = new List<int>(players.Length + 1);
		foreach (Player p in players) {
			potentialTargets.Add(p.playerID);
		}
		potentialTargets.Add(-1);
		for (int targetType = 0; targetType < (int)CardTarget.CARD_TARGET_MAX; ++targetType) {
			if ((targetMask & (1 << targetType)) != 0) {
				potentialTargets = Intersect(potentialTargets, GetPotentialTargets((CardTarget)targetType, from, inheritedTarget));
			}
		}
		return potentialTargets;
	}

	// gather the list of players that can be targeted with the given type
	List<Player> GetPossibleTargets(int targetMask) {
		List<int> targetPlayerIDs = FindTargetsFromMask(targetMask, GameplayManager.SharedInstance.Game.LocalPlayer.Id, -1);
		List<Player> potentialPlayers = new List<Player>(targetPlayerIDs.Count);
		foreach (int playerID in targetPlayerIDs) {
			foreach (Player p in players) {
				if (p.playerID == playerID) {
					potentialPlayers.Add(p);
				}
			}
		}
		return potentialPlayers;
	}

	void OnTouchMove(Vector3 point) {
		if (!touchEnabled || viewingCard) {
			return;
		}

		float distance = (new Vector2 (point.x, point.y) - new Vector2 (mouseDownStartPosition.x, mouseDownStartPosition.y)).sqrMagnitude;
		point.z = this.transform.position.z;
		this.transform.position = point;
		this.GetComponent<Canvas>().sortingOrder = 100;

		if (faceDown) {
			foreach (Player p in GetPossibleTargets((1 << (int)CardTarget.CARD_TARGET_OTHER_PLAYER))) {
				// Highlight players
				CanvasGroup c = p.transform.FindChild ("Player Highlight").GetComponent<CanvasGroup> ();
				c.alpha = 1;
				p.transform.FindChild ("Player Highlight").GetComponent<CanvasGroup> ().alpha = c.alpha;
			}
		} else if (!zoomed) {
			// Move to touch location
			if (distance > moveThreshold) {
				isMoving = true;
			}

			HighlightTargets();
		} else {
			if (distance > moveThreshold) {
				this.transform.localScale = startScale;
				this.transform.localPosition = startPosition;
				this.GetComponent<Canvas>().sortingOrder = startLayer;
				zoomed = false;	
				isMoving = true;
				HighlightTargets();
			} else {
				this.transform.position = zoomedPosition;
			}
		}
	}
	
	void OnTouchUp(Vector3 point) {
		if (!touchEnabled) {
			return;
		}

		Debug.Log ("Plot Card released");

		isMoving = false;
		
		if (!faceDown) {
			// Zoom in on card if not already
			if (!zoomed) {
				if (AssetModel.Target == (1 << (int)CardTarget.CARD_TARGET_NONE)) {
					float distance = (new Vector2(generalCardSlot.transform.position.x, generalCardSlot.transform.position.y) - new Vector2(this.transform.position.x, this.transform.position.y)).sqrMagnitude;
					if (distance < 50500.0f) {
						GameplayManager.QueuePlotCard(Model, -1, (cmdResult) => {
							if (cmdResult.IsError) {
								this.transform.localPosition = startPosition;
								this.GetComponent<Canvas>().sortingOrder = startLayer;
								Debug.Log(System.String.Format("Failed to play card: {0}", cmdResult.ErrorMessage));
								ApplicationManager.SharedInstance.ShowPopupErrorWithTitle("Failed to Play Card", cmdResult.ErrorMessage);
							}
						});
					}	else {
						this.transform.localPosition = startPosition;
						this.GetComponent<Canvas>().sortingOrder = startLayer;
					}
				} else {
					bool foundTarget = false;
					foreach (Player p in GetPossibleTargets(AssetModel.Target)) {
						float distance = (new Vector2(p.transform.position.x, p.transform.position.y) - new Vector2(this.transform.position.x, this.transform.position.y)).sqrMagnitude;
						if (distance < snapDistance) {
							GameplayManager.QueuePlotCard(Model, p.playerID, (cmdResult) => {
								if (cmdResult.IsError) {
									this.transform.localPosition = startPosition;
									this.GetComponent<Canvas>().sortingOrder = startLayer;
									ApplicationManager.SharedInstance.ShowPopupErrorWithTitle("Failed to Play Card", cmdResult.ErrorMessage);
									Debug.Log(System.String.Format("Failed to play card: {0}", cmdResult.ErrorMessage));
								}
							});
							foundTarget = true;
							break;
						} 
					}

				if (!foundTarget) {
						this.transform.localPosition = startPosition;
						this.GetComponent<Canvas>().sortingOrder = startLayer;
					}
				}
			} else {
				// Revert card to normal size
				this.transform.localScale = startScale;
				this.transform.localPosition = startPosition;
				this.GetComponent<Canvas>().sortingOrder = startLayer;
				zoomed = false;	
			}
		}	else {
			foreach (Player p in GetPossibleTargets((1 << (int)CardTarget.CARD_TARGET_OTHER_PLAYER))) {
				float distance = (new Vector2(p.transform.position.x, p.transform.position.y) - new Vector2(this.transform.position.x, this.transform.position.y)).sqrMagnitude;
				if (distance < snapDistance) {
					// Send card to player
					Vector2 oldStartPos = startPosition;
					HideFromView();
					ResetStartPosition();
					GameplayManager.GiveCardToPlayer(p.playerID, (cmdResult) => {
						if (cmdResult.IsError) {
							ApplicationManager.SharedInstance.ShowPopupErrorWithTitle("Unable to Give Card", cmdResult.ErrorMessage);
							this.transform.position = oldStartPos;
							startPosition = oldStartPos;
						}
					});
				}
			}
			this.transform.position = startPosition;
			this.GetComponent<Canvas>().sortingOrder = startLayer;
		}
		
		// Un-highlight card slots
		for (int i = 0; i < playerCardSlots.Length; i++){
			CanvasGroup c = playerCardSlots[i].GetComponent<CanvasGroup>();
			c.alpha = 0;
			playerCardSlots[i].GetComponent<CanvasGroup>().alpha = c.alpha;
		}
	}

	public void HideFromView() {
		transform.position = new Vector3(-1000, 0);
	}

	void OnTouchDown(Vector3 point){
		if (touchEnabled) {
			mouseDownStartPosition = point;
			lastTouchDownTime = Time.time;
		}
	}
	
	void OnTouchCancel(){
			this.transform.localPosition = startPosition;
			this.transform.localScale = startScale;
			this.GetComponent<Canvas>().sortingOrder = startLayer;
			
			for (int i = 0; i < playerCardSlots.Length; i++) {
				playerCardSlots[i].GetComponent<CanvasGroup>().alpha = 0;
			}
	}
	
	void OnTouchStay(Vector3 point) {
		if (touchEnabled) {
			if (!faceDown) {
				if (!zoomed) {
					if ((Time.time - lastTouchDownTime) > heldThreshold) {
						if (!isMoving) {
							this.transform.localScale = zoomedScale;
							this.transform.position = zoomedPosition;
							this.GetComponent<Canvas>().sortingOrder = 15;
							zoomed = true;
						}
					}
				}
			}
		}
	}
	
	void ResetStartPosition() {
		startPosition = transform.localPosition;
	}
		
	void LoadMaterial() {
		//From testing changing material on mesh
		cardMesh = this.GetComponent<MeshRenderer>();
		replacementMat =  Resources.Load("Materials/Test Card Material") as Material;
		cardMesh.material = replacementMat;
	}

	public void SetCardModel(CardModel cardModel) {
		Model = cardModel;
		AssetModel = GameAssetManager.SharedInstance.Cards[Model.AssetId];
		
		Transform[] children = transform.FindChild ("Image").GetComponentsInChildren<Transform>();
		
		foreach(Transform child in children)
		{
			if(child.name == "Title") {
				title = child.gameObject.GetComponent<Text>();
				if (title != null) {
					title.text = AssetModel.Name;
				}
			}
			if(child.name == "Description") {
				description = child.gameObject.GetComponent<Text>();
				if (description != null) {
				description.text = AssetModel.Description;
				}
			}
			if(child.name == "Phase") {
				tooltip = child.gameObject.GetComponent<Text>();
				if (tooltip != null) {
					tooltip.text = "";
				}
			}
			if(child.name == "Cost") {
				cost = child.gameObject.GetComponent<Text>();
				if (cost != null) {
					//Debug.Log(AssetModel.Cost);
					cost.text = AssetModel.Cost.ToString();
				}
			}
		}
		SetImage();
	}

	void SetImage() {
		switch (AssetModel.Rarity){
			case CardRarity.CARD_RARITY_NONE:
				break;
			case CardRarity.CARD_RARITY_COMMON:
					transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().AnyPhase;
				break;
			case CardRarity.CARD_RARITY_UNCOMMON:
					transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().Counter;
				break;
			case CardRarity.CARD_RARITY_EPIC:
					transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().MissionPhase;
				break;
			case CardRarity.CARD_RARITY_RARE:
					transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().VotePhase;
			break;
		}
		
		transform.FindChild("Image").GetComponent<FXActivation>().otherImage = transform.FindChild("Image").GetComponent<Image>().sprite;
	}

	public void FlipImage()
	{
		if (title != null) {
			title.text = "";
		}

		if (description != null) {
			description.text = "";
		}

		if (cost != null) {
			cost.text = "";
		}

		if (AssetModel.Hidden) {
			this.transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().CardBackHidden;
		} else {
			this.transform.FindChild("Image").GetComponent<Image>().sprite = GameObject.Find("CardManager").GetComponent<CardManager>().CardBack;
		}
	}

	void HightLightDistributedCards() {
		CanvasGroup c;
		c = this.transform.FindChild("Image").FindChild("CardHighlight").GetComponent<CanvasGroup>();

		if (faceDown && touchEnabled) {
			c.alpha = 1;
			this.transform.FindChild("Image").FindChild("CardHighlight").localScale = new Vector3(0.93f, 0.93f, 1.0f);
		} 
		
		this.transform.FindChild ("Image/CardHighlight").GetComponent<CanvasGroup> ().alpha = c.alpha;
	}

	public void UpdateCardHighlight() {
		CanvasGroup c;
		
		c = this.transform.FindChild("Image").FindChild("CardHighlight").GetComponent<CanvasGroup>();
		c.alpha = 0;
		
		if (AssetModel.Phase != CardPhase.CARD_PHASE_COUNTER) {
			switch (GameplayManager.SharedInstance.Game.GameState.State) {
			case GameState.STATE_PROPOSE_TEAM:
				if (AssetModel.Phase == CardPhase.CARD_PHASE_TEAM_BUILD || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_PROPOSE_TEAM_COMPLETE: 
				if (AssetModel.Phase == CardPhase.CARD_PHASE_TEAM_BUILD || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_VOTE_FOR_TEAM:  
				if (AssetModel.Phase == CardPhase.CARD_PHASE_TEAM_VOTE || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_VOTE_FOR_TEAM_COMPLETE:
				if (AssetModel.Phase == CardPhase.CARD_PHASE_TEAM_VOTE || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_MISSION_PLAY:    
				if (AssetModel.Phase == CardPhase.CARD_PHASE_MISSION || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_MISSION_PLAY_COMPLETE:
				if (AssetModel.Phase == CardPhase.CARD_PHASE_MISSION || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			case GameState.STATE_MISSION_OVER:  
				if (AssetModel.Phase == CardPhase.CARD_PHASE_MISSION || AssetModel.Phase == CardPhase.CARD_PHASE_ANY) {
					c.alpha = 1;
				}
				break;
			}
			
			
		} else {
			if (GameplayManager.SharedInstance.Game.CardState.State == CardState.CARD_STATE_ATTEMPTING) {
				c.alpha = 1;
			} 
		}

		this.transform.FindChild ("Image/CardHighlight").GetComponent<CanvasGroup> ().alpha = c.alpha;
	}

	void HighlightTargets() {
		if (AssetModel) {
			if (AssetModel.Target == (1 << (int)CardTarget.CARD_TARGET_NONE)) {

			} else {
				foreach (Player p in GetPossibleTargets(AssetModel.Target)) {
					// Highlight players
					CanvasGroup c = p.transform.FindChild ("Player Highlight").GetComponent<CanvasGroup> ();
					c.alpha = 1;
					p.transform.FindChild ("Player Highlight").GetComponent<CanvasGroup> ().alpha = c.alpha;
				}
			}
		}
	}
}

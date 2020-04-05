﻿using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// For Gateways inheritable class
/// </summary>
public class StationGateway : NetworkBehaviour
{
	[SerializeField]
	private SpriteRenderer[] Sprites;
	//SpriteBaseBottom, SpriteBaseTop, SpriteBaseRightMiddle, SpriteBaseLeftMiddle, SpriteBaseRightBottom, SpriteBaseLeftBottom, SpriteBaseRightTop, SpriteBaseLeftTop, SpriteBaseCentre
	//TODO animate centre
	[SerializeField]
	private SpriteRenderer Centre;

	private int Count = 0;

	[SerializeField]
	private Sprite[] Online;
	[SerializeField]
	private Sprite[] Offline;
	[SerializeField]
	private Sprite[] PowerOff;//TODO connect gateway to APC

	[SerializeField]
	private List<GameObject> Worlds = new List<GameObject>();//List of worlds available to be chosen

	private GameObject SelectedWorld;// The world from the list that was chosen

	private bool HasPower = true;// Not used atm

	private bool IsConnected;

	[SerializeField]
	private int RandomCountBegining = 300; //Defaults to between 5 and 20 mins gate will open.
	[SerializeField]
	private int RandomCountEnd = 1200;

	private protected RegisterTile registerTile;

	private Matrix Matrix => registerTile.Matrix;

	public string WorldName = "The Station";

	private protected Vector3Int Position;

	private protected string Message;

	private protected float timeElapsedServer = 0;
	private protected float timeElapsedClient = 0;
	public float DetectionTime = 1;

	[SyncVar(hook = nameof(SyncState))]
	private bool isOn = false;

	private void SyncState(bool oldVar, bool newVar)
	{
		isOn = newVar;
		//do your thing
		//all clients will be updated with this
	}

	[Server]
	public void ServerChangeState(bool newVar)
	{
		isOn = newVar;
	}

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > DetectionTime && isOn)
			{
				DetectPlayer();
				timeElapsedServer = 0;
			}
		}
		else
		{
			timeElapsedClient += Time.deltaTime;
			if (timeElapsedClient > 1)
			{
				if (isOn)
				{
					SetOnline();
				}
				else
				{
					SetOffline();
				}
				timeElapsedClient = 0;
			}
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void Start()
	{
		SetOffline();

		if (!isServer) return;

		registerTile = GetComponent<RegisterTile>();
		Position = registerTile.WorldPosition;

		ServerChangeState(false);

		var count = Random.Range(RandomCountBegining, RandomCountEnd);
		Invoke(nameof(WorldSetup), count);
	}

	[Server]
	public virtual void WorldSetup()
	{
		//Selects Random world
		SelectedWorld = Worlds[Random.Range(0, Worlds.Count)];

		if (SelectedWorld == null) return;

		var selectedWorld = SelectedWorld.GetComponent<WorldGateway>();

		Message = "Teleporting to: " + selectedWorld.WorldName;

		if (!selectedWorld.IsOnlineAtStart)
		{
			selectedWorld.IsOnlineAtStart = true;
			selectedWorld.SetUp();
		}

		if (HasPower)
		{
			SetOnline();
			ServerChangeState(true);

			var text = "Alert! New Gateway connection formed.\n\n Connection established to: " + SelectedWorld.GetComponent<WorldGateway>().WorldName;
			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
		}
	}

	[Server]
	public virtual void DetectPlayer()
	{
		//detect players positioned on the portal bit of the gateway
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Player, true);

		foreach (ObjectBehaviour player in playersFound)
		{
			var coord = new Vector2(Position.x, Position.y);
			Chat.AddLocalMsgToChat(Message, coord, gameObject);
			SoundManager.PlayNetworkedForPlayer(player.gameObject, "StealthOff"); //very weird, sometimes does the sound other times not.
			TransportPlayers(player);
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Object, true))
		{
			TransportObjectsItems(objects);
		}

		foreach (var items in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Item, true))
		{
			TransportObjectsItems(items);
		}
	}

	[Server]
	public virtual void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(SelectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	[Server]
	public virtual void TransportObjectsItems(ObjectBehaviour objectsitems)
	{
		objectsitems.GetComponent<CustomNetTransform>().SetPosition(SelectedWorld.GetComponent<RegisterTile>().WorldPosition);
	}

	public virtual void SetOnline()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = Online[i];
		}

		Centre.sprite = Online[Count + 8];
		Count += 1;

		if (Count > 2)
		{
			Count = 0;
		}
	}

	public virtual void SetOffline()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = Offline[i];
		}

		Centre.sprite = Offline[Count + 8];
		Count += 1;

		if (Count > 2)
		{
			Count = 0;
		}
	}

	public virtual void SetPowerOff()
	{
		for (int i = 0; i < Sprites.Length; i++)
		{
			Sprites[i].sprite = PowerOff[i];
		}
	}
}
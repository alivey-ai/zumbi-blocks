using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_matchdata : MonoBehaviour
{
	public int wave;

	public int minutes;

	public int seconds;

	public int zombieskilled;

	public bool isstarted;

	public bool isended;

	public float sendrate;

	public float dataconter;

	public GUISkin guiskin;

	public GUISkin guiskin2;

	public GUISkin chatskin;

	public bool senddata;

	public Transform gamecontrol;

	public Color[] chatcolors;

	public int[] themessagecor;

	public string[] themessage;

	public int messages;

	public float mess_cont;

	public string[] playernames;

	public int[] playerpings;

	public int[] playerkills;

	public int[] playermoneys;

	public bool[] playeralive;

	public int playersingame;

	public sc_matchdata()
	{
		sendrate = 2f;
	}

	public void Start()
	{
		gamecontrol = GameObject.Find("gamecontroler").transform;
		((Component)this).NetView().RPC("askthewave", (RPCMode)0, new object[0]);
	}

	[RPC]
	public void askthewave()
	{
		((Component)this).NetView().RPC("syncwave", (RPCMode)2, new object[1] { wave });
	}

	public void addmessage(string mes, int cor)
	{
		mess_cont = 0f;
		int num = 0;
		if (messages == themessage.Length)
		{
			erasemessage(0);
			themessagecor[messages] = cor;
			themessage[messages] = mes;
			messages++;
		}
		else
		{
			themessagecor[messages] = cor;
			themessage[messages] = mes;
			messages++;
		}
	}

	public void erasemessage(int num)
	{
		while (num < themessage.Length - 1)
		{
			themessage[num] = themessage[num + 1];
			themessagecor[num] = themessagecor[num + 1];
			num++;
		}
		messages--;
	}

	public void Update()
	{
		if (senddata)
		{
			dataconter += 1000f * Time.deltaTime * sendrate;
			if (!(dataconter <= 1000f))
			{
				((Component)this).NetView().RPC("sendstuff", (RPCMode)1, new object[5] { minutes, seconds, zombieskilled, isstarted, isended });
				dataconter -= 1000f;
			}
		}
		if (messages > 0)
		{
			mess_cont += 0.1f * Time.deltaTime;
			if (!(mess_cont <= 1f))
			{
				erasemessage(0);
				mess_cont = 0f;
			}
		}
	}

	public void OnGUI()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		string text = null;
		string text2 = null;
		int num = 0;
		text = string.Empty + minutes;
		text2 = ((seconds >= 10) ? (string.Empty + seconds) : ("0" + seconds));
		GUI.skin = guiskin;
		GUI.color = Color.yellow;
		GUI.Label(new Rect((float)(Screen.width / 2 - 200), 10f, 400f, 40f), text + ":" + text2);
		GUI.skin = chatskin;
		GUI.color = Color.white;
		for (num = 0; num < messages; num++)
		{
			GUI.color = chatcolors[themessagecor[num]];
			if (num == 0)
			{
				float a = 1f - mess_cont;
				Color color = GUI.color;
				float num2 = (color.a = a);
				Color val = (GUI.color = color);
			}
			else
			{
				int num3 = 1;
				Color color2 = GUI.color;
				float num4 = (color2.a = num3);
				Color val3 = (GUI.color = color2);
			}
			GUI.Label(new Rect(5f, (float)(25 + num * 15), 500f, 25f), themessage[num]);
		}
		GUI.color = Color.white;
	}

	[RPC]
	public void syncwave(int thewave)
	{
		wave = thewave;
	}

	[RPC]
	public void syncinfo(int[] RPCkills, int[] RPCmoneys, int[] RPCpings, int RPCplayers)
	{
		for (int i = 0; i < RPCplayers; i++)
		{
			playerkills[i] = RPCkills[i];
			playerpings[i] = RPCpings[i];
			playermoneys[i] = RPCmoneys[i];
		}
		playersingame = RPCplayers;
	}

	[RPC]
	public void syncnames(string RPCname, int id)
	{
		playernames[id] = RPCname;
	}

	[RPC]
	public void addzombiedeath()
	{
		zombieskilled++;
	}

	[RPC]
	public void sendstuff(int RPCminutes, int RPCseconds, int RPCzombieskilled, bool RPCisstarted, bool RPCisended)
	{
		minutes = RPCminutes;
		seconds = RPCseconds;
		zombieskilled = RPCzombieskilled;
		isstarted = RPCisstarted;
		isended = RPCisended;
	}

	public void Main()
	{
	}
}

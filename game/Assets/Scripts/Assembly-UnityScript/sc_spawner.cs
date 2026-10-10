using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_spawner : MonoBehaviour
{
	public int timetospawn;

	public Transform playerprefab;

	public int state;

	public Transform scenecontrol;

	public Transform gamecontrol;

	public Transform playerobj;

	public bool canspawn;

	public float conter;

	public GUISkin guiskin;

	public GUISkin guiskin2;

	public GUISkin guiSlist;

	public GUISkin otherfont;

	public GUISkin chatfont;

	public GUISkin guiInv;

	public GUISkin guiInv2;

	public Texture cubesprite;

	public Texture blackcube;

	public Texture[] inv3_sign;

	public bool justlogged;

	public string playername;

	public float mousesense;

	public bool autoaim;

	public bool autoreload;

	public GameObject matchcontroller;

	public Transform matchdata;

	public int myid;

	public GameObject[] spawns;

	public int[] inve;

	public int money;

	public int kills;

	public bool spawnei;

	public float BM_alpha;

	public string BM_message;

	public string chat;

	public bool chatbool;

	public GameObject[] specplayers;

	public int spectarget;

	public float specdist;

	public Vector3 camfix;

	public int my_beard;

	public int my_beardcolor;

	public int my_shirtcolor;

	public int my_skincolor;

	public string[] chatcommands;

	public int send_cmd;

	public string parsefrase;

	public int onmouse;

	public bool mouseup;

	public sc_spawner()
	{
		timetospawn = 5;
		state = 1;
		justlogged = true;
		chatbool = true;
		specdist = 4f;
		my_beard = -1;
		send_cmd = -1;
		onmouse = -1;
	}

	[RPC]
	public void servermessage(string mess)
	{
		BM_message = mess;
		BM_alpha = 4f;
	}

	[RPC]
	public void addkill()
	{
		kills++;
	}

	// host leaves, level B+: the owner restores its kill count of the old match (ZbCarry.OnSpawn)
	[RPC]
	public void zbkills(int k)
	{
		kills = k;
	}

	[RPC]
	public void addmoney(int quant)
	{
		money += quant;
	}

	[RPC]
	public void receivechat(string mes, int cor)
	{
		if (((Component)this).NetView().isMine)
		{
			GameObject val = null;
			val = GameObject.Find("matchdata(Clone)");
			if ((Object)(object)val != (Object)null)
			{
				((sc_matchdata)(object)val.GetComponent(typeof(sc_matchdata))).addmessage(mes, cor);
			}
		}
	}

	[RPC]
	public void dropitem(int id, Vector3 pos, Quaternion rot, int quant, int ammo)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)matchcontroller == (Object)null)
		{
			matchcontroller = GameObject.Find("matchcontroller(Clone)");
		}
		if ((Object)(object)matchcontroller != (Object)null)
		{
			((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).spawnitem(id, pos, rot, quant, ammo, true);
		}
	}

	[RPC]
	public void sendchat(string mes, int cor)
	{
		if ((Object)(object)matchcontroller == (Object)null)
		{
			matchcontroller = GameObject.Find("matchcontroller(Clone)");
		}
		if ((Object)(object)matchcontroller != (Object)null)
		{
			((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).spreadchat(mes, cor);
		}
	}

	[RPC]
	public void asksuplly()
	{
		if ((Object)(object)matchcontroller == (Object)null)
		{
			matchcontroller = GameObject.Find("matchcontroller(Clone)");
		}
		if ((Object)(object)matchcontroller != (Object)null)
		{
			((sc_matchcontroller)(object)matchcontroller.GetComponent(typeof(sc_matchcontroller))).spawnsuplly();
		}
	}

	[RPC]
	public void endgame(int RPCscore, int RPCsecs, int RPCmins)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (((Component)this).NetView().isMine)
		{
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).lastscore = RPCscore;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).timesurvivedsecs = RPCsecs;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).timesurvivedmins = RPCmins;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.parent = null;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localPosition = Vector3.zero;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localEulerAngles = Vector3.zero;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).disconecplease = true;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate = 12;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money + money;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).lastaccmoney = money;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).save();
			Application.LoadLevel(2);
		}
	}

	[RPC]
	public void updatemypermission()
	{
		if ((Object)(object)GameObject.Find("matchcontroller(Clone)") != (Object)null)
		{
			((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).updatepermission(((Component)this).gameObject);
		}
	}

	[RPC]
	public void addmyspawner()
	{
		if ((Object)(object)GameObject.Find("matchcontroller(Clone)") != (Object)null)
		{
			((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).addspawner(((Component)this).gameObject);
		}
	}

	[RPC]
	public void setpermission(bool permission, int id)
	{
		myid = id;
		canspawn = permission;
	}

	[RPC]
	public void updatename(string name)
	{
		playername = name;
	}

	public void Start()
	{
		specplayers = GameObject.FindGameObjectsWithTag("Player");
		Input.eatKeyPressOnTextFieldFocus = false;
		if (!((Component)this).NetView().isMine)
		{
			((Behaviour)this).enabled = false;
		}
		scenecontrol = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).scenecontroller;
		((Component)this).NetView().RPC("updatename", (RPCMode)0, new object[1] { playername });
		((Component)this).NetView().RPC("addmyspawner", (RPCMode)0, new object[0]);
	}

	public void Update()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Expected O, but got Unknown
		//IL_0dcb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dde: Unknown result type (might be due to invalid IL or missing references)
		//IL_0deb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e10: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e17: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e88: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e93: Unknown result type (might be due to invalid IL or missing references)
		mouseup = Input.GetKeyUp((KeyCode)323);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int result = 0;
		int num5 = 0;
		Transform val = null;
		Transform val2 = null;
		RaycastHit val3 = default(RaycastHit);
		LayerMask val4 = default(LayerMask);
		chatbool = true;
		if (!canspawn && (Object)(object)playerobj == (Object)null)
		{
			((Component)this).NetView().RPC("updatemypermission", (RPCMode)0, new object[0]);
		}
		if (state == 1)
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			Transform val5 = null;
			float num6 = 0f;
			float num7 = 0f;
			if (canspawn && (Object)(object)playerobj == (Object)null)
			{
				conter += 100f * Time.deltaTime;
				if (!(conter <= (float)(100 * timetospawn)) && !spawnei)
				{
					spawnei = true;
					spawns = GameObject.FindGameObjectsWithTag("playerspawn");
					num = Random.Range(0, spawns.Length);
					justlogged = false;
					num6 = spawns[num].transform.position.x + Random.Range(0f - spawns[num].transform.localScale.x / 2f, spawns[num].transform.localScale.x / 2f);
					num7 = spawns[num].transform.position.z + Random.Range(0f - spawns[num].transform.localScale.z / 2f, spawns[num].transform.localScale.z / 2f);
					val5 = (Transform)Network.Instantiate((Object)(object)playerprefab, new Vector3(num6, spawns[num].transform.position.y, num7), playerprefab.rotation, 0);
					for (num2 = 0; num2 < inve.Length; num2++)
					{
						((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).item[num2] = inve[num2];
						if (inve[num2] >= 0)
						{
							if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[inve[num2]].tipo == 0)
							{
								((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).quant[num2] = ((sc_gunscript)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[inve[num2]].prefab).GetComponent(typeof(sc_gunscript))).maxammo;
								((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).ammo[num2] = ((sc_gunscript)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[inve[num2]].prefab).GetComponent(typeof(sc_gunscript))).maxammo;
							}
							if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[inve[num2]].tipo == 4)
							{
								((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).quant[num2] = 1;
								((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).ammo[num2] = 1;
							}
						}
					}
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).myskin = my_skincolor;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).beardcolor = my_beardcolor;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).shirtcolor = my_shirtcolor;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).beardnum = my_beard;
					if (((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).myskin != 0)
					{
						((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).beardcolor = 2;
					}
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).cam = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).gamecontrol = gamecontrol;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).playername = playername;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).mouse_sense = mousesense;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).myid = myid;
					((Component)(sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).NetView().RPC("setid", (RPCMode)1, new object[1] { myid });
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).autoreload = autoreload;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).autoaim = autoaim;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).myspawner = ((Component)this).transform;
					((sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).controled = true;
					((Component)val5).GetComponent<Rigidbody>().isKinematic = false;
					((Behaviour)(sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))).enabled = true;
					((Behaviour)(sc_onlinecontroller)(object)((Component)val5).GetComponent(typeof(sc_onlinecontroller))).enabled = false;
					playerobj = val5;
					ZbCarry.OnSpawn(this, (sc_player)(object)((Component)val5).GetComponent(typeof(sc_player))); // host leaves, level B+: my items / health from the old match
					canspawn = false;
					conter = 0f;
				}
			}
			if (Input.GetKeyDown((KeyCode)27))
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				state = 2;
				if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
				{
					((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = false;
				}
			}
			if (Input.GetKeyDown((KeyCode)105) && (Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
			{
				state = 5;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = false;
			}
			if (Input.GetKeyUp((KeyCode)116))
			{
				state = 4;
				chatbool = false;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				GUI.FocusControl("chatbox");
			}
		}
		if (state == 5 && Input.GetKeyDown((KeyCode)27))
		{
			state = 1;
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
			{
				((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = true;
			}
		}
		if (state == 2 && Input.GetKeyUp((KeyCode)116))
		{
			state = 4;
			chatbool = false;
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			GUI.FocusControl("chatbox");
		}
		if (state == 4)
		{
			if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
			{
				((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = false;
			}
			if (chatbool && Input.GetKeyDown((KeyCode)13))
			{
				state = 1;
				if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
				{
					((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = true;
				}
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
				if (chat.Length > 0)
				{
					if (Network.isClient)
					{
						((Component)this).NetView().RPC("sendchat", (RPCMode)0, new object[2]
						{
							playername + ": " + chat,
							0
						});
					}
					else
					{
						send_cmd = -1;
						for (num2 = 0; num2 < chatcommands.Length; num2++)
						{
							result = 1;
							for (num3 = 0; num3 < chatcommands[num2].Length; num3++)
							{
								if (num3 >= chat.Length)
								{
									result = 0;
									break;
								}
								if (chatcommands[num2][num3] != chat[num3])
								{
									result = 0;
								}
							}
							if (result == 1)
							{
								send_cmd = num2;
								break;
							}
						}
						if (send_cmd == -1)
						{
							sendchat(playername + ": " + chat, 1);
						}
						else
						{
							if (send_cmd == 0)
							{
								parsefrase = chat.Substring(chatcommands[0].Length);
								if (int.TryParse(parsefrase, out result))
								{
									result = int.Parse(parsefrase);
									((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).kick_player(result);
								}
							}
							if (send_cmd == 1)
							{
								parsefrase = chat.Substring(chatcommands[1].Length);
								if (int.TryParse(parsefrase, out result))
								{
									result = int.Parse(parsefrase);
									((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).ban_player_ip(result);
								}
							}
							if (send_cmd == 2)
							{
								((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).server_say(chat.Substring(chatcommands[2].Length));
							}
							if (send_cmd == 3)
							{
								Network.incomingPassword = chat.Substring(chatcommands[3].Length);
								((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).spreadchat("Server has changed password", 5);
							}
							if (send_cmd == 4)
							{
								parsefrase = chat.Substring(chatcommands[4].Length);
								if (int.TryParse(parsefrase, out result))
								{
									result = int.Parse(parsefrase);
									((sc_matchcontroller)(object)GameObject.Find("matchcontroller(Clone)").GetComponent(typeof(sc_matchcontroller))).disban_player_ip(result);
								}
							}
						}
					}
				}
				chat = string.Empty;
			}
			if (Input.GetKeyDown((KeyCode)27))
			{
				state = 1;
				if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
				{
					((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = true;
				}
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
		}
		if (!((Object)(object)playerobj == (Object)null))
		{
			return;
		}
		if (specplayers == null)
		{
			specplayers = GameObject.FindGameObjectsWithTag("Player");
			spectarget = 0;
			return;
		}
		if (Input.GetKeyDown((KeyCode)323))
		{
			spectarget++;
			if (spectarget > specplayers.Length - 1)
			{
				spectarget = 0;
			}
		}
		if (specplayers.Length > 0)
		{
			if ((Object)(object)specplayers[spectarget] == (Object)null)
			{
				specplayers = GameObject.FindGameObjectsWithTag("Player");
				spectarget = 0;
				return;
			}
			val2 = ((sc_scenecontroller)(object)((Component)((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator;
			val = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam;
			val2.position = specplayers[spectarget].transform.position;
			camfix = Vector3.Lerp(camfix, val2.position, 5f * Time.deltaTime);
			val.position = camfix;
			val4 = (256);
			if (Physics.Raycast(val.position, -val.forward, out val3, 4f, (val4)))
			{
				specdist = Mathf.Lerp(specdist, val3.distance * 0.99f, 15f * Time.deltaTime);
			}
			else
			{
				specdist = Mathf.Lerp(specdist, 3.67f, 15f * Time.deltaTime);
			}
			val.Translate(-Vector3.forward * specdist);
		}
		else
		{
			specplayers = GameObject.FindGameObjectsWithTag("Player");
			spectarget = 0;
		}
	}

	public void OnGUI()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_16e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_219c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1768: Unknown result type (might be due to invalid IL or missing references)
		//IL_176d: Unknown result type (might be due to invalid IL or missing references)
		//IL_176e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1781: Unknown result type (might be due to invalid IL or missing references)
		//IL_1783: Unknown result type (might be due to invalid IL or missing references)
		//IL_1784: Unknown result type (might be due to invalid IL or missing references)
		//IL_178b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1739: Unknown result type (might be due to invalid IL or missing references)
		//IL_173e: Unknown result type (might be due to invalid IL or missing references)
		//IL_173f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1751: Unknown result type (might be due to invalid IL or missing references)
		//IL_1753: Unknown result type (might be due to invalid IL or missing references)
		//IL_1754: Unknown result type (might be due to invalid IL or missing references)
		//IL_175b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_237d: Unknown result type (might be due to invalid IL or missing references)
		//IL_23f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_245c: Unknown result type (might be due to invalid IL or missing references)
		//IL_2270: Unknown result type (might be due to invalid IL or missing references)
		//IL_17b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_273c: Unknown result type (might be due to invalid IL or missing references)
		//IL_2540: Unknown result type (might be due to invalid IL or missing references)
		//IL_2569: Unknown result type (might be due to invalid IL or missing references)
		//IL_22e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f80: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f93: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f96: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1808: Unknown result type (might be due to invalid IL or missing references)
		//IL_180d: Unknown result type (might be due to invalid IL or missing references)
		//IL_180e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1820: Unknown result type (might be due to invalid IL or missing references)
		//IL_1822: Unknown result type (might be due to invalid IL or missing references)
		//IL_1823: Unknown result type (might be due to invalid IL or missing references)
		//IL_182a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1855: Unknown result type (might be due to invalid IL or missing references)
		//IL_1869: Unknown result type (might be due to invalid IL or missing references)
		//IL_186e: Unknown result type (might be due to invalid IL or missing references)
		//IL_186f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1882: Unknown result type (might be due to invalid IL or missing references)
		//IL_1884: Unknown result type (might be due to invalid IL or missing references)
		//IL_1885: Unknown result type (might be due to invalid IL or missing references)
		//IL_188c: Unknown result type (might be due to invalid IL or missing references)
		//IL_18b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_18e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1919: Unknown result type (might be due to invalid IL or missing references)
		//IL_194d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1981: Unknown result type (might be due to invalid IL or missing references)
		//IL_071a: Unknown result type (might be due to invalid IL or missing references)
		//IL_071f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0720: Unknown result type (might be due to invalid IL or missing references)
		//IL_0732: Unknown result type (might be due to invalid IL or missing references)
		//IL_0734: Unknown result type (might be due to invalid IL or missing references)
		//IL_0735: Unknown result type (might be due to invalid IL or missing references)
		//IL_073c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_19aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_19af: Unknown result type (might be due to invalid IL or missing references)
		//IL_19b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_19c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_19c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_19c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_19cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ab4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b09: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b22: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b27: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b28: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b45: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b65: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0992: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0764: Unknown result type (might be due to invalid IL or missing references)
		//IL_0777: Unknown result type (might be due to invalid IL or missing references)
		//IL_077c: Unknown result type (might be due to invalid IL or missing references)
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_078f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0791: Unknown result type (might be due to invalid IL or missing references)
		//IL_0792: Unknown result type (might be due to invalid IL or missing references)
		//IL_0799: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0701: Unknown result type (might be due to invalid IL or missing references)
		//IL_0702: Unknown result type (might be due to invalid IL or missing references)
		//IL_0709: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e6b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e87: Unknown result type (might be due to invalid IL or missing references)
		//IL_09dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_135c: Unknown result type (might be due to invalid IL or missing references)
		//IL_136f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1374: Unknown result type (might be due to invalid IL or missing references)
		//IL_1375: Unknown result type (might be due to invalid IL or missing references)
		//IL_1387: Unknown result type (might be due to invalid IL or missing references)
		//IL_1389: Unknown result type (might be due to invalid IL or missing references)
		//IL_138a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1391: Unknown result type (might be due to invalid IL or missing references)
		//IL_13b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_13d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_13d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_13d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_13f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1112: Unknown result type (might be due to invalid IL or missing references)
		//IL_1117: Unknown result type (might be due to invalid IL or missing references)
		//IL_111b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d10: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d11: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d23: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d25: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d26: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d74: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d79: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d8d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d90: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d97: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e57: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ea6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f07: Unknown result type (might be due to invalid IL or missing references)
		//IL_2112: Unknown result type (might be due to invalid IL or missing references)
		//IL_209a: Unknown result type (might be due to invalid IL or missing references)
		//IL_200c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0804: Unknown result type (might be due to invalid IL or missing references)
		//IL_0813: Unknown result type (might be due to invalid IL or missing references)
		//IL_0818: Unknown result type (might be due to invalid IL or missing references)
		//IL_0819: Unknown result type (might be due to invalid IL or missing references)
		//IL_082c: Unknown result type (might be due to invalid IL or missing references)
		//IL_082e: Unknown result type (might be due to invalid IL or missing references)
		//IL_082f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0836: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eaf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eda: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e38: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e54: Unknown result type (might be due to invalid IL or missing references)
		//IL_1422: Unknown result type (might be due to invalid IL or missing references)
		//IL_145a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1495: Unknown result type (might be due to invalid IL or missing references)
		//IL_14bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_14c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_14c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_14d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_14df: Unknown result type (might be due to invalid IL or missing references)
		//IL_1517: Unknown result type (might be due to invalid IL or missing references)
		//IL_1565: Unknown result type (might be due to invalid IL or missing references)
		//IL_15b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_1260: Unknown result type (might be due to invalid IL or missing references)
		//IL_1270: Unknown result type (might be due to invalid IL or missing references)
		//IL_127a: Unknown result type (might be due to invalid IL or missing references)
		//IL_127f: Unknown result type (might be due to invalid IL or missing references)
		//IL_128a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1190: Unknown result type (might be due to invalid IL or missing references)
		//IL_11a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_11aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_11af: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f7c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1606: Unknown result type (might be due to invalid IL or missing references)
		//IL_160b: Unknown result type (might be due to invalid IL or missing references)
		//IL_160c: Unknown result type (might be due to invalid IL or missing references)
		//IL_161f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1621: Unknown result type (might be due to invalid IL or missing references)
		//IL_1622: Unknown result type (might be due to invalid IL or missing references)
		//IL_1629: Unknown result type (might be due to invalid IL or missing references)
		//IL_169c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1099: Unknown result type (might be due to invalid IL or missing references)
		GUI.skin = guiskin;
		int num = 0;
		int num2 = 0;
		Vector3 val = default(Vector3);
		Texture val2 = null;
		if (state == 5)
		{
			if ((Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) == (Object)null)
			{
				state = 1;
			}
			if ((Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
			{
				if ((Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) == (Object)null)
				{
					state = 1;
				}
				else
				{
					GUI.color = Color.white;
					float a = 0.7f;
					Color color = GUI.color;
					float num3 = (color.a = a);
					Color val3 = (GUI.color = color);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 230), (float)(Screen.height / 2 - 180), 460f, 280f), blackcube, (ScaleMode)0, true, 0f);
					float a2 = 0.1f;
					Color color2 = GUI.color;
					float num4 = (color2.a = a2);
					Color val5 = (GUI.color = color2);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 230), (float)(Screen.height / 2 - 180), 460f, 30f), cubesprite, (ScaleMode)0, true, 0f);
					GUI.skin = guiInv;
					GUI.color = Color.yellow;
					GUI.Label(new Rect((float)(Screen.width / 2 - 150), (float)(Screen.height / 2 - 180), 300f, 25f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[60]);
					GUI.skin = guiskin;
					val = Input.mousePosition;
					val.y = (float)Screen.height - Input.mousePosition.y;
					bool flag = default(bool);
					for (num2 = 0; num2 < 5; num2++)
					{
						GUI.color = Color.white;
						Rect val7 = new Rect((float)(Screen.width / 2 - 20), (float)(Screen.height / 2 - 120 + 35 * num2), 96f, 33f);
						if (val7.Contains(val))
						{
							float a3 = 0.2f;
							Color color3 = GUI.color;
							float num5 = (color3.a = a3);
							Color val8 = (GUI.color = color3);
							if (Input.GetKeyDown((KeyCode)323) && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0 && num2 != ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
							{
								onmouse = num2;
							}
							if (onmouse >= 0 && mouseup && onmouse != num2)
							{
								flag = false;
								if (((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] >= 0 && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0)
								{
									num = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].tipo;
									if ((num == 3 || num == 4 || num == 5) && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] == ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2])
									{
										flag = true;
									}
								}
								if (num2 != ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
								{
									if (flag)
									{
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] + ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse] = 0;
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] = -1;
									}
									else
									{
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] = num;
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[num2] = num;
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] = num;
									}
								}
							}
							GUI.color = Color.white;
							float a4 = 0.2f;
							Color color4 = GUI.color;
							float num6 = (color4.a = a4);
							Color val10 = (GUI.color = color4);
						}
						else
						{
							float a5 = 0.1f;
							Color color5 = GUI.color;
							float num7 = (color5.a = a5);
							Color val12 = (GUI.color = color5);
						}
						if (num2 == ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
						{
							GUI.color = Color.red;
							float a6 = 0.4f;
							Color color6 = GUI.color;
							float num8 = (color6.a = a6);
							Color val14 = (GUI.color = color6);
						}
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 20), (float)(Screen.height / 2 - 120 + 35 * num2), 96f, 33f), cubesprite, (ScaleMode)0, true, 0f);
						if (((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0 && onmouse != num2)
						{
							GUI.color = Color.white;
							int num9 = 1;
							Color color7 = GUI.color;
							float num10 = (color7.a = num9);
							Color val16 = (GUI.color = color7);
							val2 = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].sprite;
							GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 20), (float)(Screen.height / 2 - 120 + 35 * num2), (float)val2.width, (float)val2.height), val2, (ScaleMode)0, true, 0f);
							num = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].tipo;
							if (num == 3 || num == 4 || num == 5)
							{
								GUI.skin = guiInv2;
								GUI.Label(new Rect((float)(Screen.width / 2 - 20), (float)(Screen.height / 2 - 117 + 35 * num2), (float)val2.width, (float)val2.height), "x" + ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2]);
							}
						}
					}
					for (; num2 < 10; num2++)
					{
						GUI.color = Color.white;
						Rect val18 = new Rect((float)(Screen.width / 2 + 95), (float)(Screen.height / 2 - 120 + 35 * (num2 - 5)), 96f, 33f);
						if (val18.Contains(val))
						{
							float a7 = 0.2f;
							Color color8 = GUI.color;
							float num11 = (color8.a = a7);
							Color val19 = (GUI.color = color8);
							if (Input.GetKeyDown((KeyCode)323) && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0 && num2 != ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
							{
								onmouse = num2;
							}
							if (onmouse >= 0 && mouseup && onmouse != num2)
							{
								flag = false;
								if (((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] >= 0 && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0)
								{
									num = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].tipo;
									if ((num == 3 || num == 4 || num == 5) && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] == ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2])
									{
										flag = true;
									}
								}
								if (num2 != ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
								{
									if (flag)
									{
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] + ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse] = 0;
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] = -1;
									}
									else
									{
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] = num;
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[num2] = num;
										num = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse] = ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2];
										((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2] = num;
									}
								}
							}
							GUI.color = Color.white;
							float a8 = 0.2f;
							Color color9 = GUI.color;
							float num12 = (color9.a = a8);
							Color val21 = (GUI.color = color9);
						}
						else
						{
							float a9 = 0.1f;
							Color color10 = GUI.color;
							float num13 = (color10.a = a9);
							Color val23 = (GUI.color = color10);
						}
						if (num2 == ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).itemselected)
						{
							GUI.color = Color.red;
							float a10 = 0.4f;
							Color color11 = GUI.color;
							float num14 = (color11.a = a10);
							Color val25 = (GUI.color = color11);
						}
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 95), (float)(Screen.height / 2 - 120 + 35 * (num2 - 5)), 96f, 33f), cubesprite, (ScaleMode)0, true, 0f);
						if (((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2] >= 0 && onmouse != num2)
						{
							GUI.color = Color.white;
							int num15 = 1;
							Color color12 = GUI.color;
							float num16 = (color12.a = num15);
							Color val27 = (GUI.color = color12);
							val2 = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].sprite;
							GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 95), (float)(Screen.height / 2 - 120 + 35 * (num2 - 5)), (float)val2.width, (float)val2.height), val2, (ScaleMode)0, true, 0f);
							num = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[num2]].tipo;
							if (num == 3 || num == 4 || num == 5)
							{
								GUI.skin = guiInv2;
								GUI.Label(new Rect((float)(Screen.width / 2 + 95), (float)(Screen.height / 2 - 117 + 35 * (num2 - 5)), (float)val2.width, (float)val2.height), "x" + ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[num2]);
							}
						}
					}
					if (mouseup)
					{
						Rect val29 = new Rect((float)(Screen.width / 2 - 230), (float)(Screen.height / 2 - 180), 460f, 280f);
						if (val29.Contains(val))
						{
							onmouse = -1;
						}
						else if (onmouse >= 0)
						{
							if (Network.isClient)
							{
								((Component)this).NetView().RPC("dropitem", (RPCMode)0, new object[5]
								{
									((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse],
									playerobj.position + ((Component)playerobj).transform.forward * 0.4f,
									((Component)this).transform.rotation,
									((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse],
									((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse]
								});
							}
							else
							{
								dropitem(((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse], playerobj.position + ((Component)playerobj).transform.forward * 0.4f, ((Component)this).transform.rotation, ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse], ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse]);
							}
							((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] = -1;
							((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).quant[onmouse] = 0;
							((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).ammo[onmouse] = 0;
							onmouse = -1;
						}
					}
					GUI.color = Color.white;
					float a11 = 0.4f;
					Color color13 = GUI.color;
					float num17 = (color13.a = a11);
					Color val30 = (GUI.color = color13);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 40), (float)(Screen.height / 2 - 130), 1f, 195f), cubesprite, (ScaleMode)0, true, 0f);
					float a12 = 0.1f;
					Color color14 = GUI.color;
					float num18 = (color14.a = a12);
					Color val32 = (GUI.color = color14);
					for (num2 = 0; num2 < 3; num2++)
					{
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 160), (float)(Screen.height / 2 - 120 + 61 * num2), 96f, 33f), cubesprite, (ScaleMode)0, true, 0f);
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 120 + 61 * num2), 33f, 33f), cubesprite, (ScaleMode)0, true, 0f);
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 82 + 61 * num2), 135f, 12f), cubesprite, (ScaleMode)0, true, 0f);
					}
					float a13 = 0.2f;
					Color color15 = GUI.color;
					float num19 = (color15.a = a13);
					Color val34 = (GUI.color = color15);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 198), (float)(Screen.height / 2 - 115), (float)inv3_sign[0].width, (float)inv3_sign[0].height), inv3_sign[0], (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 194), (float)(Screen.height / 2 - 118 + 61), (float)inv3_sign[1].width, (float)inv3_sign[1].height), inv3_sign[1], (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 195), (float)(Screen.height / 2 - 118 + 122), (float)inv3_sign[2].width, (float)inv3_sign[2].height), inv3_sign[2], (ScaleMode)0, true, 0f);
					if (onmouse >= 0 && ((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse] >= 0)
					{
						int num20 = 1;
						Color color16 = GUI.color;
						float num21 = (color16.a = num20);
						Color val36 = (GUI.color = color16);
						val2 = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).itemlist[((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).item[onmouse]].sprite;
						GUI.DrawTexture(new Rect(val.x - 48f, val.y - 16f, (float)val2.width, (float)val2.height), val2, (ScaleMode)0, true, 0f);
					}
				}
			}
		}
		if (state == 4)
		{
			GUI.skin = chatfont;
			GUI.SetNextControlName("chatbox");
			GUI.FocusControl("chatbox");
			chat = GUI.TextField(new Rect(3f, 3f, 320f, 19f), chat.ToLower(), 30);
			GUI.skin = guiskin;
		}
		if (state == 1)
		{
			GUI.skin = otherfont;
			if (!(BM_alpha >= 1f))
			{
				float bM_alpha = BM_alpha;
				Color color17 = GUI.color;
				float num22 = (color17.a = bM_alpha);
				Color val38 = (GUI.color = color17);
			}
			else
			{
				int num23 = 1;
				Color color18 = GUI.color;
				float num24 = (color18.a = num23);
				Color val40 = (GUI.color = color18);
			}
			GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 100), 600f, 200f), BM_message);
			if (!(BM_alpha <= 0f))
			{
				BM_alpha -= 0.6f * Time.deltaTime;
			}
			if (Input.GetKey((KeyCode)9))
			{
				GUI.skin = guiSlist;
				float a14 = 0.4f;
				Color color19 = GUI.color;
				float num25 = (color19.a = a14);
				Color val42 = (GUI.color = color19);
				GUI.Box(new Rect((float)(Screen.width / 2 - 310), (float)(Screen.height / 2 - 270), 620f, 500f), string.Empty);
				int num26 = 1;
				Color color20 = GUI.color;
				float num27 = (color20.a = num26);
				Color val44 = (GUI.color = color20);
				GUI.Label(new Rect((float)(Screen.width / 2 - 265), (float)(Screen.height / 2 - 234), 290f, 25f), "Name");
				GUI.Label(new Rect((float)(Screen.width / 2 + 40), (float)(Screen.height / 2 - 234), 290f, 25f), "Kill");
				GUI.Label(new Rect((float)(Screen.width / 2 + 130), (float)(Screen.height / 2 - 234), 290f, 25f), "$$$");
				GUI.Label(new Rect((float)(Screen.width / 2 - 295), (float)(Screen.height / 2 - 234), 290f, 25f), "Id");
				GUI.Label(new Rect((float)(Screen.width / 2 + 245), (float)(Screen.height / 2 - 234), 290f, 25f), "Ping");
				if ((Object)(object)matchdata != (Object)null)
				{
					float a15 = 0.5f;
					Color color21 = GUI.color;
					float num28 = (color21.a = a15);
					Color val46 = (GUI.color = color21);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 20), (float)(Screen.height / 2 - 210), 1f, (float)(27 * ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame)), cubesprite, (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 120), (float)(Screen.height / 2 - 210), 1f, (float)(27 * ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame)), cubesprite, (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 270), (float)(Screen.height / 2 - 210), 1f, (float)(27 * ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame)), cubesprite, (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 240), (float)(Screen.height / 2 - 210), 1f, (float)(27 * ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame)), cubesprite, (ScaleMode)0, true, 0f);
					int num29 = 1;
					Color color22 = GUI.color;
					float num30 = (color22.a = num29);
					Color val48 = (GUI.color = color22);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 0), (float)(Screen.height / 2 - 265), 1f, 27f), cubesprite, (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 210), 600f, 1f), cubesprite, (ScaleMode)0, true, 0f);
					GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 235), 600f, 1f), cubesprite, (ScaleMode)0, true, 0f);
					GUI.Label(new Rect((float)(Screen.width / 2 - 270), (float)(Screen.height / 2 - 265), 250f, 25f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[71]);
					GUI.Label(new Rect((float)(Screen.width / 2 + 30), (float)(Screen.height / 2 - 265), 250f, 25f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[72] + ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).wave);
					for (num2 = 0; num2 < ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame; num2++)
					{
						float a16 = 0.2f;
						Color color23 = GUI.color;
						float num31 = (color23.a = a16);
						Color val50 = (GUI.color = color23);
						GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 210 + (num2 + 1) * 27), 600f, 1f), cubesprite, (ScaleMode)0, true, 0f);
						int num32 = 1;
						Color color24 = GUI.color;
						float num33 = (color24.a = num32);
						Color val52 = (GUI.color = color24);
						GUI.Label(new Rect((float)(Screen.width / 2 + 250), (float)(Screen.height / 2 - 208 + num2 * 27), 50f, 25f), ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerpings[num2].ToString());
						GUI.Label(new Rect((float)(Screen.width / 2 - 297), (float)(Screen.height / 2 - 208 + num2 * 27), 50f, 25f), num2.ToString());
						GUI.Label(new Rect((float)(Screen.width / 2 - 265), (float)(Screen.height / 2 - 208 + num2 * 27), 290f, 25f), ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num2]);
						GUI.Label(new Rect((float)(Screen.width / 2 + 30), (float)(Screen.height / 2 - 207 + num2 * 27), 75f, 25f), string.Empty + ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills[num2]);
						GUI.Label(new Rect((float)(Screen.width / 2 + 130), (float)(Screen.height / 2 - 207 + num2 * 27), 75f, 25f), string.Empty + ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys[num2]);
					}
				}
			}
			GUI.skin = guiskin;
			int num34 = 1;
			Color color25 = GUI.color;
			float num35 = (color25.a = num34);
			Color val54 = (GUI.color = color25);
			if ((Object)(object)playerobj == (Object)null && !Input.GetKey((KeyCode)9))
			{
				if (canspawn)
				{
					if (justlogged)
					{
						num = (int)((float)timetospawn - conter / 100f + 1f);
						GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 50), 600f, 100f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[32] + num);
					}
				}
				else if (justlogged)
				{
					GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 50), 600f, 100f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[36]);
				}
				else
				{
					GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 50), 600f, 100f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[37]);
				}
			}
		}
		if (state == 2)
		{
			GUI.skin = guiskin2;
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[38]))
			{
				state = 1;
				if ((Object)(object)playerobj != (Object)null && (Object)(object)(sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player)) != (Object)null)
				{
					((sc_player)(object)((Component)playerobj).GetComponent(typeof(sc_player))).controled = true;
				}
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 0), 400f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[2]))
			{
				ZbMenu.overlay = true; // phase 3: the pause panel's Options button did nothing in the original; it opens the settings
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 35), 400f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[39]))
			{
				state = 3;
			}
		}
		if (state != 3)
		{
			return;
		}
		GUI.skin = guiskin2;
		GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 70), 400f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[40]);
		GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[41]);
		if (GUI.Button(new Rect((float)(Screen.width / 2 - 100), (float)(Screen.height / 2 - 0), 90f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[42]))
		{
			((Component)this).NetView().RPC("sendchat", (RPCMode)0, new object[2]
			{
				playername + " disconnected." + chat,
				4
			});
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.parent = null;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localPosition = Vector3.zero;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localEulerAngles = Vector3.zero;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate = 7;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).lastaccmoney = money;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money + money;
			((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).save();
			if (((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).iamserver)
			{
				((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).errormsg = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[44];
			}
			else
			{
				((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).errormsg = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[45];
			}
			Application.LoadLevel(2);
			Network.Disconnect(500);
		}
		if (GUI.Button(new Rect((float)(Screen.width / 2 - 10), (float)(Screen.height / 2 - 0), 90f, 32f), ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).language[((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).languageset].frase[43]))
		{
			state = 2;
		}
		if (Input.GetKeyDown((KeyCode)27))
		{
			state = 2;
		}
	}

	public void Main()
	{
	}
}

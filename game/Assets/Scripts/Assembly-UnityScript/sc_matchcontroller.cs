using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_matchcontroller : MonoBehaviour
{
	public int secstoincrease;

	public float rateincrease;

	public int wave;

	public int ismax;

	public GameObject[] itemspawnpoints;

	public float itemspawnrate;

	public float itemspawncont;

	public float zspawnrate;

	public float zspawncont;

	public GameObject[] zsinscene;

	public GameObject[] playersinscene;

	public GameObject[] bodiesinscene;

	public GameObject[] isinscene;

	public int zsmax;

	public int zminforwave;

	public GameObject[] zspawnpoints;

	public Transform zumbifab;

	public Transform terra;

	public GameObject[] bagspawnpoints;

	public GameObject[] everybodyspawners;

	public int spawners;

	public Transform gamecontrol;

	public int minute;

	public int second;

	public float timecont;

	public int increasesecs;

	public bool gamestarted;

	public Transform matchdata;

	public Transform myspawner;

	public bool gameended;

	public float endgameconter;

	public int secstoendgame;

	public Transform[] grounditens;

	public bool separator_of_metal;

	public Transform supllyprefab;

	public Transform ammoboxprefab;

	public Transform[] itemsrand;

	public int[] itensprob;

	public int probmax;

	public int probmin;

	public int backpackprobmax;

	public int backpackprobmin;

	public float matinfoconter;

	public float matinfosendconter;

	public float endconter;

	public float longwaveconter;

	public string[] bannedip;

	public int bannedips;

	public bool canlongwave;

	public int randomcont;

	public Transform[] contructions;

	public sc_matchcontroller()
	{
		secstoincrease = 30;
		rateincrease = 0.5f;
		ismax = 15;
		itemspawnrate = 0.05f;
		zspawnrate = 0.2f;
		zsmax = 50;
		zminforwave = 15;
		secstoendgame = 3;
		canlongwave = true;
	}

	public void generate_bulding(int bid, int ownerid, Vector3 pos, Quaternion rot)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		Transform val = (Transform)Network.Instantiate((Object)(object)contructions[bid], pos, rot, 10);
		((sc_haveowner)(object)((Component)val).GetComponent(typeof(sc_haveowner))).id = ownerid;
	}

	public void OnPlayerConnected(NetworkPlayer pl)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < bannedips; i++)
		{
			if (pl.ipAddress == bannedip[i])
			{
				Network.CloseConnection(pl, true);
				break;
			}
		}
	}

	public void disban_player_ip(int n)
	{
		if (n < bannedips)
		{
			((sc_spawner)(object)everybodyspawners[0].GetComponent(typeof(sc_spawner))).receivechat("Ubanned: " + bannedip[n], 5);
			bannedip[n] = bannedip[bannedips - 1];
			bannedips--;
		}
		else
		{
			spreadchat("Invalid IP Id!", 2);
		}
	}

	public void ban_player_ip(int p)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (p >= spawners)
		{
			return;
		}
		if (everybodyspawners[p].NetView().owner != Network.player)
		{
			if (bannedips < bannedip.Length)
			{
				spreadchat("Admin banned " + ((sc_spawner)(object)everybodyspawners[p].GetComponent(typeof(sc_spawner))).playername, 2);
				string[] array = bannedip;
				int num = bannedips;
				NetworkPlayer owner = everybodyspawners[p].NetView().owner;
				array[num] = owner.ipAddress;
				Network.CloseConnection(everybodyspawners[p].NetView().owner, true);
				bannedips++;
			}
			else
			{
				((sc_spawner)(object)everybodyspawners[0].GetComponent(typeof(sc_spawner))).receivechat("Blacklist is full!", 2);
			}
		}
		else
		{
			((sc_spawner)(object)everybodyspawners[0].GetComponent(typeof(sc_spawner))).receivechat("Cant ban yourself!", 2);
		}
	}

	public void kick_player(int p)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if (p < spawners)
		{
			if (everybodyspawners[p].NetView().owner != Network.player)
			{
				spreadchat("Admin kicked " + ((sc_spawner)(object)everybodyspawners[p].GetComponent(typeof(sc_spawner))).playername, 2);
				Network.CloseConnection(everybodyspawners[p].NetView().owner, true);
			}
			else
			{
				((sc_spawner)(object)everybodyspawners[0].GetComponent(typeof(sc_spawner))).receivechat("Cant kick yourself!", 2);
			}
		}
	}

	public void server_say(string mes)
	{
		for (int i = 0; i < spawners; i++)
		{
			everybodyspawners[i].NetView().RPC("servermessage", (RPCMode)2, new object[1] { mes });
		}
	}

	public void spreadchat(string mes, int cor)
	{
		for (int i = 0; i < spawners; i++)
		{
			everybodyspawners[i].NetView().RPC("receivechat", (RPCMode)2, new object[2] { mes, cor });
		}
	}

	public void spawnsuplly()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if (((Component)this).NetView().isMine)
		{
			Transform val = null;
			bagspawnpoints = GameObject.FindGameObjectsWithTag("bagspawn");
			int num = Random.Range(0, bagspawnpoints.Length);
			Vector2 val2 = new Vector2
			{
				x = bagspawnpoints[num].transform.position.x,
				y = bagspawnpoints[num].transform.position.z
			};
			val = (Transform)Network.Instantiate((Object)(object)supllyprefab, new Vector3(val2.x, bagspawnpoints[num].transform.position.y, val2.y), bagspawnpoints[num].transform.rotation, 5);
			float y = val.position.y + ((sc_suplly)(object)((Component)val).GetComponent(typeof(sc_suplly))).height;
			Vector3 position = val.position;
			float num2 = (position.y = y);
			Vector3 val3 = (val.position = position);
			((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
			((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
		}
	}

	public void spawnitem(int id, Vector3 pos, Quaternion rot, int quant, int ammo, bool decay)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		Transform val = null;
		if (id >= 0)
		{
			val = (Transform)Network.Instantiate((Object)(object)grounditens[id], pos, rot, 6);
			((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
			((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
			((sc_canbetaken)(object)((Component)val).GetComponent(typeof(sc_canbetaken))).decaytime = 30000f;
			((sc_canbetaken)(object)((Component)val).GetComponent(typeof(sc_canbetaken))).quant = quant;
			((sc_canbetaken)(object)((Component)val).GetComponent(typeof(sc_canbetaken))).ammo = ammo;
			((sc_canbetaken)(object)((Component)val).GetComponent(typeof(sc_canbetaken))).decay = decay;
		}
	}

	public void updatepermission(GameObject Fspawner)
	{
		int i = 0;
		bool flag = false;
		for (; i < spawners; i++)
		{
			if ((Object)(object)everybodyspawners[i].gameObject == (Object)(object)Fspawner)
			{
				Fspawner.NetView().RPC("setpermission", (RPCMode)6, new object[2] { true, i });
				flag = true;
				i = spawners;
			}
		}
		if (!flag)
		{
			addspawner(Fspawner);
		}
	}

	public void addspawner(GameObject Fspawner)
	{
		int num = 0;
		if (spawners < everybodyspawners.Length)
		{
			everybodyspawners[spawners] = Fspawner;
			everybodyspawners[spawners].NetView().RPC("setpermission", (RPCMode)6, new object[2] { true, spawners });
			spawners++;
			spreadchat(((sc_spawner)(object)everybodyspawners[spawners - 1].GetComponent(typeof(sc_spawner))).playername + " connected.", 3);
			for (num = 0; num < spawners; num++)
			{
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).playername;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).kills;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).money;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame = spawners;
			}
			for (num = 0; num < spawners; num++)
			{
				((Component)matchdata).NetView().RPC("syncnames", (RPCMode)1, new object[2]
				{
					((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num],
					num
				});
			}
			((Component)matchdata).NetView().RPC("syncinfo", (RPCMode)1, new object[4]
			{
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerpings,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame
			});
		}
	}

	public void removespawner(int Fspawner)
	{
		int num = 0;
		everybodyspawners[Fspawner] = everybodyspawners[spawners - 1];
		if ((Object)(object)everybodyspawners[Fspawner] != (Object)null)
		{
			everybodyspawners[Fspawner].NetView().RPC("setpermission", (RPCMode)2, new object[2] { true, Fspawner });
		}
		spawners--;
		for (num = 0; num < spawners; num++)
		{
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).playername;
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).kills;
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys[num] = ((sc_spawner)(object)everybodyspawners[num].GetComponent(typeof(sc_spawner))).money;
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame = spawners;
		}
		for (num = 0; num < spawners; num++)
		{
			((Component)matchdata).NetView().RPC("syncnames", (RPCMode)1, new object[2]
			{
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num],
				num
			});
		}
		((Component)matchdata).NetView().RPC("syncinfo", (RPCMode)1, new object[4]
		{
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills,
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys,
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerpings,
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame
		});
	}

	public void Start()
	{
		zspawnpoints = GameObject.FindGameObjectsWithTag("zombiespawn");
		itemspawnpoints = GameObject.FindGameObjectsWithTag("itemspawn");
		bagspawnpoints = GameObject.FindGameObjectsWithTag("bagspawn");
	}

	public void Update()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Expected O, but got Unknown
		//IL_0e69: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e92: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e9: Expected O, but got Unknown
		//IL_0767: Unknown result type (might be due to invalid IL or missing references)
		//IL_076c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0782: Unknown result type (might be due to invalid IL or missing references)
		//IL_0787: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0800: Unknown result type (might be due to invalid IL or missing references)
		//IL_0805: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0832: Unknown result type (might be due to invalid IL or missing references)
		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
		//IL_084e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0859: Unknown result type (might be due to invalid IL or missing references)
		//IL_0860: Expected O, but got Unknown
		//IL_0aba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0abf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0acf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af5: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		float num4 = 0f;
		float num5 = 0f;
		int num6 = 0;
		Transform val = null;
		bool flag = default(bool);
		matinfoconter += 50f * Time.deltaTime;
		if (!(matinfoconter <= 100f))
		{
			for (num6 = 0; num6 < spawners; num6++)
			{
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerpings[num6] = Network.GetAveragePing(everybodyspawners[num6].NetView().owner);
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playernames[num6] = ((sc_spawner)(object)everybodyspawners[num6].GetComponent(typeof(sc_spawner))).playername;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills[num6] = ((sc_spawner)(object)everybodyspawners[num6].GetComponent(typeof(sc_spawner))).kills;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys[num6] = ((sc_spawner)(object)everybodyspawners[num6].GetComponent(typeof(sc_spawner))).money;
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame = spawners;
			}
			matinfoconter = 0f;
		}
		matinfosendconter += 100f * Time.deltaTime;
		if (!(matinfosendconter <= 75f))
		{
			((Component)matchdata).NetView().RPC("syncinfo", (RPCMode)1, new object[4]
			{
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerkills,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playermoneys,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playerpings,
				((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).playersingame
			});
			matinfosendconter = 0f;
		}
		for (num6 = 0; num6 < spawners; num6++)
		{
			if ((Object)(object)everybodyspawners[num6] == (Object)null)
			{
				removespawner(num6);
				num6--;
			}
		}
		if (gamestarted)
		{
			ZbMenu.ApplyLoot(this); // playtest feature: Loot More (host room option); Classic returns at once and leaves the prefab values alone
			itemspawncont += 1000f * itemspawnrate * Time.deltaTime;
			if (!(itemspawncont <= 1000f))
			{
				isinscene = GameObject.FindGameObjectsWithTag("grounditem");
				if (isinscene.Length < ismax)
				{
					num = Random.Range(0, itemspawnpoints.Length);
					num4 = itemspawnpoints[num].transform.position.x + Random.Range(0f - itemspawnpoints[num].transform.localScale.x / 2f, itemspawnpoints[num].transform.localScale.x / 2f);
					num5 = itemspawnpoints[num].transform.position.z + Random.Range(0f - itemspawnpoints[num].transform.localScale.z / 2f, itemspawnpoints[num].transform.localScale.z / 2f);
					num2 = Random.Range(0, probmax);
					if (num2 < probmin)
					{
						randomcont = 0;
						for (num6 = 0; num6 < itemsrand.Length; num6++)
						{
							randomcont += itensprob[num6];
						}
						num3 = Random.Range(0, randomcont);
						randomcont = 0;
						for (num6 = 0; num6 < itemsrand.Length; num6++)
						{
							if (num3 >= randomcont && num3 < randomcont + itensprob[num6])
							{
								num3 = num6;
								break;
							}
							randomcont += itensprob[num6];
						}
						val = (Transform)Network.Instantiate((Object)(object)itemsrand[num3], new Vector3(num4, itemspawnpoints[num].transform.position.y + 1.28f, num5), itemsrand[num3].rotation, 5);
						((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
						((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
					}
					else
					{
						val = (Transform)Network.Instantiate((Object)(object)ammoboxprefab, new Vector3(num4, itemspawnpoints[num].transform.position.y + 1.28f, num5), ammoboxprefab.rotation, 5);
						((Behaviour)(sc_updateposition)(object)((Component)val).GetComponent(typeof(sc_updateposition))).enabled = false;
						((Behaviour)(sc_sendpositionupdate)(object)((Component)val).GetComponent(typeof(sc_sendpositionupdate))).enabled = true;
					}
					isinscene = GameObject.FindGameObjectsWithTag("grounditem");
					itemspawncont = 0f;
				}
			}
			zsinscene = GameObject.FindGameObjectsWithTag("zumbi");
			// Timed waves (playtest feature, default; ZbMenu.TimedWaves()): the 120 s "long wave" timer of the original is armed for EVERY wave.
			// The original arms it only if the previous wave was cleared (canlongwave) and disarms it after a timed-out wave. Classic = exactly that.
			bool timedwaves = ZbMenu.TimedWaves();
			if (canlongwave || timedwaves)
			{
				longwaveconter += 1f * Time.deltaTime;
			}
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).waveleft = timedwaves ? Mathf.Max(0, Mathf.CeilToInt(120f - longwaveconter)) : (-1);
			zspawncont += 1f * Time.deltaTime;
			if (zsinscene.Length <= 3 || !(longwaveconter <= 120f))
			{
				if (zsinscene.Length <= 3)
				{
					canlongwave = true;
				}
				else
				{
					canlongwave = false;
				}
				wave++;
				((Component)matchdata).NetView().RPC("syncwave", (RPCMode)2, new object[1] { wave });
				longwaveconter = 0f;
				spreadchat("Wave " + wave + " started!", 5);
				zspawncont = 0f;
				for (num6 = 0; num6 < spawners; num6++)
				{
					if ((Object)(object)everybodyspawners[num6] != (Object)null)
					{
						everybodyspawners[num6].NetView().RPC("servermessage", (RPCMode)2, new object[1] { "Wave " + wave });
					}
				}
				int zcount = ZbMenu.WaveZombies(zminforwave + (wave - 1), ZbMenu.Players()); // Timed waves: scaled with the players in the match (Classic: unchanged)
				int zcap = ZbMenu.ZombieCap(zsmax, ZbMenu.Players());
				for (num6 = 0; num6 < zcount; num6++)
				{
					if (zsinscene.Length < zcap)
					{
						if (zspawnpoints.Length > 0)
						{
							num = Random.Range(0, zspawnpoints.Length);
						}
						num4 = zspawnpoints[num].transform.position.x + Random.Range(0f - zspawnpoints[num].transform.localScale.x / 2f, zspawnpoints[num].transform.localScale.x / 2f);
						num5 = zspawnpoints[num].transform.position.z + Random.Range(0f - zspawnpoints[num].transform.localScale.z / 2f, zspawnpoints[num].transform.localScale.z / 2f);
						val = (Transform)Network.Instantiate((Object)(object)zumbifab, new Vector3(num4, zspawnpoints[num].transform.position.y + 1.28f, num5), zumbifab.rotation, 5);
						((Component)val).GetComponent<Rigidbody>().isKinematic = false;
						((Behaviour)(sc_zumbionlinecontroller)(object)((Component)val).GetComponent(typeof(sc_zumbionlinecontroller))).enabled = false;
						((Behaviour)(sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).enabled = true;
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).gamecontrol = gamecontrol;
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).matchcontrol = ((Component)this).transform;
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).headlife = ((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).headlife + (float)(4 * (wave - 1));
						((sc_life)(object)((Component)val).GetComponent(typeof(sc_life))).quantia = ((sc_life)(object)((Component)val).GetComponent(typeof(sc_life))).quantia + (float)(10 * (wave - 1));
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).dmgmin = ((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).dmgmin + 0.5f * (float)(wave - 1);
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).dmgmax = ((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).dmgmax + 0.5f * (float)(wave - 1);
						((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).walkspeed = ((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).walkspeed + 0.06f * (float)(wave - 1);
						if (!(((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).walkspeed <= 6f))
						{
							((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).walkspeed = 6f;
						}
						num2 = Random.Range(0, backpackprobmax);
						if (num2 < backpackprobmin)
						{
							((sc_zumbi)(object)((Component)val).GetComponent(typeof(sc_zumbi))).carryitem = true;
						}
						Network.Instantiate((Object)(object)terra, new Vector3(val.position.x, val.position.y - 1.75f, val.position.z), val.rotation, 4);
						zspawncont = 0f;
						zsinscene = GameObject.FindGameObjectsWithTag("zumbi");
					}
				}
			}
			if (!gameended)
			{
				timecont += 1000f * Time.deltaTime;
				if (!(timecont <= 1000f))
				{
					second++;
					if (second >= 60)
					{
						minute++;
						second -= 60;
					}
					increasesecs++;
					if (increasesecs > secstoincrease)
					{
						zspawnrate += rateincrease;
						increasesecs = 0;
					}
					timecont -= 1000f;
				}
			}
			playersinscene = GameObject.FindGameObjectsWithTag("Player");
			if (playersinscene.Length == 0)
			{
				gameended = true;
			}
		}
		if (gameended)
		{
			endconter += 100f * Time.deltaTime;
			if (!(endconter <= 400f))
			{
				for (num6 = 0; num6 < spawners; num6++)
				{
					if ((Object)(object)everybodyspawners[num6] != (Object)null && !everybodyspawners[num6].NetView().isMine)
					{
						everybodyspawners[num6].NetView().RPC("endgame", (RPCMode)5, new object[3]
						{
							((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).zombieskilled,
							((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).seconds,
							((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).minutes
						});
					}
				}
				endgameconter += 1000f * Time.deltaTime;
				if (endgameconter > (float)(1000 * secstoendgame) || spawners <= 1)
				{
					Cursor.visible = true;
					Cursor.lockState = CursorLockMode.None;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).lastscore = ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).zombieskilled;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).timesurvivedsecs = ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).seconds;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).timesurvivedmins = ((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).minutes;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.parent = null;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localPosition = Vector3.zero;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).cam.localEulerAngles = Vector3.zero;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).disconecplease = true;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).gamestate = 12;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money = ((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).money + ((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).money;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).lastaccmoney = ((sc_spawner)(object)((Component)myspawner).GetComponent(typeof(sc_spawner))).money;
					((sc_gamecontrol)(object)((Component)gamecontrol).GetComponent(typeof(sc_gamecontrol))).save();
					Application.LoadLevel(2);
				}
			}
		}
		((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).minutes = minute;
		((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).seconds = second;
		((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).isstarted = gamestarted;
	}

	public void Main()
	{
	}
}

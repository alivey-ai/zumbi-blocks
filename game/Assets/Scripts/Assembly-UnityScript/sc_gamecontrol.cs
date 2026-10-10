using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

[Serializable]
public class sc_gamecontrol : MonoBehaviour
{
	public float audiovol;

	public float musicvol;

	public bool saveinlist;

	public bool testnat;

	public string buildversion;

	public string saveversion;

	public int gamestate;

	public GUISkin guiskin;

	public GUISkin guisking;

	public GUISkin guiskinerr;

	public GUISkin guiSlist;

	public GUISkin newgui_square;

	public GUISkin newgui_square2;

	public GUISkin newgui_square3;

	public Texture[] newhud;

	public string playernick;

	public int porttoconec;

	public string porttostring;

	public string the_password;

	public Transform cam;

	public Texture menulogo;

	public Texture fscreenbuton;

	public Texture SL_line;

	public float logoaspect;

	public Transform spawnerprefab;

	public Transform matchcontrollerprefab;

	public Transform matchdataprefab;

	public Transform matchdata;

	public Transform matchcontroller;

	public Transform scenecontroller;

	public string errormsg;

	public Transform[] loadingobjects;

	public bool iamserver;

	public HostData[] hostData;

	public float mousesense;

	public bool autoreload;

	public bool autoaim;

	public int lastscore;

	public int timesurvivedsecs;

	public int timesurvivedmins;

	public Texture backsprite;

	public Texture enabledsign;

	public Texture disabledsign;

	public Texture searchsign;

	public Texture ref_sign;

	public int languageset;

	public sc_linguajar[] language;

	public sc_item[] itemlist;

	public int screenlastx;

	public int screenlasty;

	public bool disconecplease;

	public bool cannat;

	public bool ispub;

	public bool canlist;

	public bool sp;

	public string[] ipstoconec;

	public int trynow;

	public int ipstotry;

	public int money;

	public int[] invent;

	public int[] itemtosell;

	public int[] shoparray;

	public int solength;

	public int shopadicional;

	public int shopselected;

	public int shoparrayselected;

	public int shoptype;

	public Texture lockicon;

	public Texture backitem;

	public int selecteditem;

	public int buyitem;

	public int juststarted;

	public Transform instedspawner;

	public int lastaccmoney;

	public Material skymaterial;

	public int[] botaosizereload;

	public int[] botaosizeaim;

	public int listinicio;

	public int listmaxroll;

	public int sharedhud;

	public Transform manequimfab;

	public Transform elmanequim;

	public int mane_w;

	public int mane_h;

	public int mane_up_cont;

	public Transform itempivofab;

	public Transform itempivo;

	public Transform item_show;

	public int pretend_resolution;

	public int pretend_vsync;

	public int pretend_antialias;

	public int pretend_texture;

	public byte[] bytearray;

	public int my_beard;

	public int my_beardcolor;

	public int my_shirtcolor;

	public int my_skincolor;

	public Transform playerprefab;

	public byte[] quadribyte;

	public bool[] octabit;

	public int onmouse;

	public Vector2 pivo_vec2;

	public bool turnoff_LM;

	public int turnoffonter;

	public string S_name;

	public bool usarNat;

	public bool tooshort_s_name;

	public int server_select;

	public ConnectionTesterStatus testvar;

	public string searching_server;

	public sc_gamecontrol()
	{
		audiovol = 1f;
		musicvol = 1f;
		saveinlist = true;
		testnat = true;
		buildversion = "alpha 0.1.0";
		saveversion = "v001";
		porttoconec = 25334;
		logoaspect = 1.8f;
		mousesense = 1f;
		languageset = 1;
		ipstotry = 1;
		money = 100;
		shopselected = -1;
		shoparrayselected = -1;
		selecteditem = -1;
		my_beard = -1;
		onmouse = -1;
	}

	public bool bytetobit(byte n)
	{
		for (int i = 0; i < 8; i++)
		{
			if ((int)((uint)n >> 1 * i) % 2 == 1)
			{
				octabit[i] = true;
			}
			else
			{
				octabit[i] = false;
			}
		}
		return true;
	}

	public bool inttobyte(int n)
	{
		for (int i = 0; i < 4; i++)
		{
			quadribyte[i] = (byte)(n >> 8 * i);
		}
		return true;
	}

	public int bytestoint(byte[] vetor, int pos)
	{
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			Debug.Log((object)Random.Range(0, 256));
			num += vetor[pos + i] << 8 * i;
		}
		return num;
	}

	public void save_onfile()
	{
		for (int i = 0; i < 10; i++)
		{
		}
	}

	public void sortby_type_price()
	{
		int num = 0;
		int i;
		for (i = 0; i < shoparray.Length; i++)
		{
			shoparray[i] = i;
		}
		i = 0;
		while (i < shoparray.Length - 1)
		{
			if (itemlist[itemtosell[shoparray[i + 1]]].tipo < itemlist[itemtosell[shoparray[i]]].tipo)
			{
				num = shoparray[i + 1];
				shoparray[i + 1] = shoparray[i];
				shoparray[i] = num;
				if (i > 0)
				{
					i--;
				}
			}
			else
			{
				i++;
			}
		}
		int num2 = 0;
		int num3 = 0;
		for (i = 0; i < shoparray.Length - 1; i++)
		{
			if (itemlist[itemtosell[shoparray[i]]].tipo <= num2)
			{
				continue;
			}
			int num4 = num3;
			while (num4 < i - 1)
			{
				if (itemlist[itemtosell[shoparray[num4 + 1]]].price < itemlist[itemtosell[shoparray[num4]]].price)
				{
					num = shoparray[num4 + 1];
					shoparray[num4 + 1] = shoparray[num4];
					shoparray[num4] = num;
					if (num4 > num3)
					{
						num4--;
					}
				}
				else
				{
					num4++;
				}
			}
			num2 = itemlist[itemtosell[shoparray[i]]].tipo;
			num3 = i;
		}
	}

	public void save()
	{
		ZbMenu.BeforeSave(this);
		PlayerPrefs.SetString("ZB_" + saveversion + "_prefname", playernick);
		PlayerPrefs.SetInt("ZB_" + saveversion + "_my_beard", my_beard);
		PlayerPrefs.SetInt("ZB_" + saveversion + "_my_beardcolor", my_beardcolor);
		PlayerPrefs.SetInt("ZB_" + saveversion + "_my_skincolor", my_skincolor);
		PlayerPrefs.SetInt("ZB_" + saveversion + "_my_shirtcolor", my_shirtcolor);
		if (autoaim)
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_autoaim", 1);
		}
		else
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_autoaim", 0);
		}
		if (autoreload)
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_autoreload", 1);
		}
		else
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_autoreload", 0);
		}
		PlayerPrefs.SetInt("ZB_" + saveversion + "_money", money);
		PlayerPrefs.SetFloat("ZB_" + saveversion + "_sound", audiovol);
		PlayerPrefs.SetFloat("ZB_" + saveversion + "_sense", mousesense);
		for (int i = 0; i < invent.Length; i++)
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_invent" + i, invent[i]);
		}
		PlayerPrefs.Save(); // WebGL port: flush to IndexedDB now, otherwise the data is lost when the tab is closed
	}

	public void load()
	{
		playernick = PlayerPrefs.GetString("ZB_" + saveversion + "_prefname", "player");
		my_beard = PlayerPrefs.GetInt("ZB_" + saveversion + "_my_beard", -1);
		my_beardcolor = PlayerPrefs.GetInt("ZB_" + saveversion + "_my_beardcolor", 0);
		my_skincolor = PlayerPrefs.GetInt("ZB_" + saveversion + "_my_skincolor", 0);
		my_shirtcolor = PlayerPrefs.GetInt("ZB_" + saveversion + "_my_shirtcolor", 0);
		if (PlayerPrefs.GetInt("ZB_" + saveversion + "_autoaim", 0) == 0)
		{
			autoaim = false;
		}
		else
		{
			autoaim = true;
		}
		if (PlayerPrefs.GetInt("ZB_" + saveversion + "_autoreload", 0) == 0)
		{
			autoreload = false;
		}
		else
		{
			autoreload = true;
		}
		money = PlayerPrefs.GetInt("ZB_" + saveversion + "_money", 0);
		for (int i = 0; i < invent.Length; i++)
		{
			invent[i] = PlayerPrefs.GetInt("ZB_" + saveversion + "_invent" + i, -1);
		}
		mousesense = PlayerPrefs.GetFloat("ZB_" + saveversion + "_sense", 1f);
		audiovol = PlayerPrefs.GetFloat("ZB_" + saveversion + "_sound", 1f);
	}

	public float cript(float number)
	{
		return number / 5f + 75f;
	}

	public float decript(float number)
	{
		return (number - 75f) * 5f;
	}

	public void Start()
	{
#if UNITY_WEBGL
		Application.targetFrameRate = -1; // WebGL: let the browser drive requestAnimationFrame (a fixed rate forces setTimeout pacing)
#else
		Application.targetFrameRate = 120;
#endif
		Application.runInBackground = true;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		Object.DontDestroyOnLoad((Object)(object)((Component)cam).gameObject);
		screenlastx = Screen.width;
		screenlasty = Screen.height;
		porttostring = string.Empty + porttoconec;
		juststarted = PlayerPrefs.GetInt("ZB_" + saveversion + "_start", 0);
		if (juststarted == 0)
		{
			PlayerPrefs.SetInt("ZB_" + saveversion + "_start", 1);
			save();
		}
		load();
		update_manequim();
		shopselected = -1;
		update_gunshow();
	}

	public void Update()
	{
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		if (turnoffonter >= 0 && turnoff_LM)
		{
			turnoffonter++;
			if (turnoffonter >= 10)
			{
				turnoffonter = -1;
				Renderer[] array = (Renderer[])(object)Object.FindObjectsOfType(typeof(Renderer));
				for (int i = 0; i < array.Length; i++)
				{
					array[i].lightmapIndex = 255;
				}
			}
		}
		if (mane_up_cont > 0)
		{
			mane_up_cont++;
			if (mane_up_cont > 5)
			{
				update_manequim();
				update_gunshow();
				mane_up_cont = 0;
			}
		}
		if ((Object)(object)GameObject.Find("matchdata(Clone)") != (Object)null)
		{
			matchdata = GameObject.Find("matchdata(Clone)").transform;
		}
		if ((Object)(object)instedspawner != (Object)null)
		{
			((sc_spawner)(object)((Component)instedspawner).GetComponent(typeof(sc_spawner))).matchdata = matchdata;
		}
		if ((Object)(object)item_show != (Object)null)
		{
			itempivo.Rotate(Vector3.up * Time.deltaTime * 50f);
		}
	}

	public bool strstr(string source, string find)
	{
		int result;
		if (find.Length > source.Length)
		{
			result = 0;
		}
		else if (find.Length == 0)
		{
			result = 1;
		}
		else
		{
			source = source.ToLower();
			find = find.ToLower();
			int num = 0;
			while (true)
			{
				if (num < source.Length - find.Length + 1)
				{
					int num2 = 0;
					while (source[num + num2] == find[num2] && num2 < find.Length)
					{
						if (num2 != find.Length - 1)
						{
							num2++;
							continue;
						}
						goto IL_005c;
					}
					num++;
					continue;
				}
				result = 0;
				break;
				IL_005c:
				result = 1;
				break;
			}
		}
		return (byte)result != 0;
	}

	public void update_gunshow()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)itempivo != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)itempivo).gameObject);
		}
		itempivo = (Transform)Object.Instantiate((Object)(object)itempivofab, ((Component)this).transform.position, ((Component)this).transform.rotation);
		Ray val = ((Component)cam).GetComponent<Camera>().ScreenPointToRay(new Vector3(120f, (float)(Screen.height - 230), 1f));
		float num = Vector2.Distance(new Vector2(0f, 0f), new Vector2((float)Screen.height, (float)Screen.width));
		num /= 1000f;
		itempivo.position += val.direction * (num * 6f);
		if ((Object)(object)item_show != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)item_show).gameObject);
		}
		if (shopselected >= 0)
		{
			item_show = (Transform)Object.Instantiate((Object)(object)itemlist[shopselected].prefab, ((Component)this).transform.position, ((Component)this).transform.rotation);
			item_show.parent = itempivo;
			item_show.localPosition = Vector3.zero;
			if ((Object)(object)(mostrador_ajuste)(object)((Component)item_show).GetComponent(typeof(mostrador_ajuste)) != (Object)null)
			{
				item_show.localPosition += ((mostrador_ajuste)(object)((Component)item_show).GetComponent(typeof(mostrador_ajuste))).posissao;
				item_show.localEulerAngles = ((mostrador_ajuste)(object)((Component)item_show).GetComponent(typeof(mostrador_ajuste))).rotassao;
			}
			else
			{
				item_show.localEulerAngles = Vector3.zero;
			}
		}
	}

	public void update_manequim()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Expected O, but got Unknown
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)elmanequim != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)elmanequim).gameObject);
		}
		mane_w = Screen.width; // WebGL port: remember the screen size the mannequin was placed for
		mane_h = Screen.height;
		elmanequim = (Transform)Object.Instantiate((Object)(object)manequimfab, ((Component)this).transform.position, ((Component)this).transform.rotation);
		Ray val = ((Component)cam).GetComponent<Camera>().ScreenPointToRay(new Vector3(120f, (float)(Screen.height - 340), 1f));
		float num = Vector2.Distance(new Vector2(0f, 0f), new Vector2((float)Screen.height, (float)Screen.width));
		num /= 1000f;
		elmanequim.position += val.direction * (num * 5f);
		for (int i = 0; i < ((sc_manequim)(object)((Component)elmanequim).GetComponent(typeof(sc_manequim))).skinpart.Length; i++)
		{
			((Component)((sc_manequim)(object)((Component)elmanequim).GetComponent(typeof(sc_manequim))).skinpart[i]).GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).skincolors[my_skincolor];
		}
		for (int i = 0; i < ((sc_manequim)(object)((Component)elmanequim).GetComponent(typeof(sc_manequim))).shirtpart.Length; i++)
		{
			((Component)((sc_manequim)(object)((Component)elmanequim).GetComponent(typeof(sc_manequim))).shirtpart[i]).GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).camisacolors[my_shirtcolor];
		}
		if (my_beard >= 0)
		{
			Transform val2 = (Transform)Object.Instantiate((Object)(object)((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).beardprefabs[my_beard]);
			val2.parent = ((sc_manequim)(object)((Component)elmanequim).GetComponent(typeof(sc_manequim))).bearpos;
			val2.position = val2.parent.position;
			val2.localEulerAngles = Vector3.zero;
			((sc_reference)(object)((Component)val2).GetComponent(typeof(sc_reference))).refer.GetComponent<Renderer>().material = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).haircolors[my_beardcolor];
		}
	}

	public void OnLevelWasLoaded()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Expected O, but got Unknown
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Expected O, but got Unknown
		if (turnoff_LM)
		{
			turnoffonter = 0;
		}
		save();
		Transform val = null;
		int num = 0;
		if (disconecplease)
		{
			Network.Disconnect(200);
			disconecplease = false;
		}
		if (gamestate == 3)
		{
			scenecontroller = GameObject.Find("scenecontrol").transform;
			((sc_scenecontroller)(object)((Component)scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator.eulerAngles = Vector3.zero;
			cam.parent = ((sc_scenecontroller)(object)((Component)scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator;
			cam.localPosition = Vector3.zero;
			cam.localEulerAngles = Vector3.zero;
			val = (instedspawner = (Transform)Network.Instantiate((Object)(object)spawnerprefab, spawnerprefab.position, spawnerprefab.rotation, 0));
			((Behaviour)(sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).enabled = true;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).mousesense = mousesense;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).autoreload = autoreload;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).autoaim = autoaim;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_beard = my_beard;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_shirtcolor = my_shirtcolor;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_skincolor = my_skincolor;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_beardcolor = my_beardcolor;
			for (num = 0; num < invent.Length; num++)
			{
				((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).inve[num] = invent[num];
			}
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).gamecontrol = ((Component)this).transform;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).canspawn = true;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).playername = playernick;
			matchcontroller = (Transform)Object.Instantiate((Object)(object)matchcontrollerprefab);
			matchdata = (Transform)Network.Instantiate((Object)(object)matchdataprefab, matchdataprefab.position, matchdataprefab.rotation, 0);
			((sc_matchdata)(object)((Component)matchdata).GetComponent(typeof(sc_matchdata))).senddata = true;
			((sc_matchcontroller)(object)((Component)matchcontroller).GetComponent(typeof(sc_matchcontroller))).matchdata = matchdata;
			((sc_matchcontroller)(object)((Component)matchcontroller).GetComponent(typeof(sc_matchcontroller))).myspawner = instedspawner;
			((sc_matchcontroller)(object)((Component)matchcontroller).GetComponent(typeof(sc_matchcontroller))).gamecontrol = ((Component)this).transform;
			((sc_matchcontroller)(object)((Component)matchcontroller).GetComponent(typeof(sc_matchcontroller))).addspawner(((Component)val).gameObject);
		}
		if (gamestate == 5)
		{
			scenecontroller = GameObject.Find("scenecontrol").transform;
			((sc_scenecontroller)(object)((Component)scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator.eulerAngles = Vector3.zero;
			Network.Connect(ipstoconec[trynow], porttoconec, the_password);
		}
		if (gamestate != 5 && gamestate != 3)
		{
			update_manequim();
		}
	}

	public void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		if (gamestate == 3)
		{
		}
		if (gamestate == 6)
		{
			lastaccmoney = 0;
			errormsg = "Either you lost connection or the server shuted down";
		}
		ZbMenu.HostLeftBank(this); // phase 4: the host left (not a lost connection): bank this match's money like the ESC Disconnect does
		if (gamestate != 12)
		{
			gamestate = 8;
		}
		cam.parent = null;
		cam.localPosition = Vector3.zero;
		cam.localEulerAngles = Vector3.zero;
		Application.LoadLevel(2);
		ZbMenu.HostLeftScreen(this); // phase 4: "host left" screen, the group continues in a new match (claim restart)
	}

	public void OnConnectedToServer()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		Transform val = null;
		int num = 0;
		if (gamestate == 5)
		{
			val = (instedspawner = (Transform)Network.Instantiate((Object)(object)spawnerprefab, spawnerprefab.position, spawnerprefab.rotation, 0));
			((Behaviour)(sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).enabled = true;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).mousesense = mousesense;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).autoreload = autoreload;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).autoaim = autoaim;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).matchdata = matchdata;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_beard = my_beard;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_shirtcolor = my_shirtcolor;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_skincolor = my_skincolor;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).my_beardcolor = my_beardcolor;
			for (num = 0; num < invent.Length; num++)
			{
				((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).inve[num] = invent[num];
			}
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).gamecontrol = ((Component)this).transform;
			((sc_spawner)(object)((Component)val).GetComponent(typeof(sc_spawner))).playername = playernick;
			cam.parent = ((sc_scenecontroller)(object)((Component)scenecontroller).GetComponent(typeof(sc_scenecontroller))).observator;
			cam.localPosition = Vector3.zero;
			cam.localEulerAngles = Vector3.zero;
			gamestate = 6;
		}
	}

	public void OnFailedToConnect(NetworkConnectionError error)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Invalid comparison between Unknown and I4
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Invalid comparison between Unknown and I4
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Invalid comparison between Unknown and I4
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Invalid comparison between Unknown and I4
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Invalid comparison between Unknown and I4
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Invalid comparison between Unknown and I4
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Invalid comparison between Unknown and I4
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Invalid comparison between Unknown and I4
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Invalid comparison between Unknown and I4
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Invalid comparison between Unknown and I4
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		if (ZbMenu.QuickFail(this, error)) // phase 3: a Quick Play join that failed searches again / creates a room
		{
			return;
		}
		cam.parent = null;
		cam.localPosition = Vector3.zero;
		cam.localEulerAngles = Vector3.zero;
		Application.LoadLevel(2);
		if (gamestate == 3)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			gamestate = 0;
		}
		if (gamestate != 5)
		{
			return;
		}
		if (trynow == ipstotry - 1)
		{
			gamestate = 27;
			errormsg = string.Empty;
			if ((int)error == 21)
			{
				errormsg = "Worng RSA public key";
			}
			if ((int)error == 23)
			{
				errormsg = "Wrong password";
			}
			if ((int)error == 15)
			{
				errormsg = "Room not found or closed (check the room code)";
			}
			if ((int)error == 18)
			{
				errormsg = "Server is full";
			}
			if ((int)error == 22)
			{
				errormsg = "You are banned";
			}
			if ((int)error == 16)
			{
				errormsg = "Already connected";
			}
			if ((int)error == -1)
			{
				errormsg = "Already connected to other";
			}
			if ((int)error == -2)
			{
				errormsg = "Failed to create socket";
			}
			if ((int)error == -3)
			{
				errormsg = "Incorrect function parameters, report that if possible";
			}
			if ((int)error == -4)
			{
				errormsg = "Empty connection target";
			}
			if ((int)error == -5)
			{
				errormsg = "Empty connection target";
			}
			if ((int)error == 69)
			{
				errormsg = "NAT Target Not Connected";
			}
			if ((int)error == 71)
			{
				errormsg = "NAT Target Connection Lost";
			}
			if ((int)error == 73)
			{
				errormsg = "NAT Punchthrough Failed";
			}
			if (errormsg == string.Empty)
			{
				errormsg = "Could not connect: Unknown Connection problem";
			}
		}
		else
		{
			trynow++;
			Network.Connect(ipstoconec[trynow], porttoconec, the_password);
		}
	}

	public void OnPlayerDisconnected(NetworkPlayer player)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}

	public void OnGUI()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0701: Unknown result type (might be due to invalid IL or missing references)
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e19: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ead: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1007: Unknown result type (might be due to invalid IL or missing references)
		//IL_1043: Unknown result type (might be due to invalid IL or missing references)
		//IL_1098: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_15c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_15ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_1627: Unknown result type (might be due to invalid IL or missing references)
		//IL_1668: Unknown result type (might be due to invalid IL or missing references)
		//IL_167e: Unknown result type (might be due to invalid IL or missing references)
		//IL_16b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_16e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1706: Unknown result type (might be due to invalid IL or missing references)
		//IL_1728: Unknown result type (might be due to invalid IL or missing references)
		//IL_1112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0751: Unknown result type (might be due to invalid IL or missing references)
		//IL_075b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0760: Unknown result type (might be due to invalid IL or missing references)
		//IL_0769: Unknown result type (might be due to invalid IL or missing references)
		//IL_076e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0799: Unknown result type (might be due to invalid IL or missing references)
		//IL_079e: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_058f: Unknown result type (might be due to invalid IL or missing references)
		//IL_240f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2456: Unknown result type (might be due to invalid IL or missing references)
		//IL_24af: Unknown result type (might be due to invalid IL or missing references)
		//IL_24ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_254f: Unknown result type (might be due to invalid IL or missing references)
		//IL_259f: Unknown result type (might be due to invalid IL or missing references)
		//IL_25ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_263f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2692: Unknown result type (might be due to invalid IL or missing references)
		//IL_118f: Unknown result type (might be due to invalid IL or missing references)
		//IL_11e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0903: Unknown result type (might be due to invalid IL or missing references)
		//IL_0905: Unknown result type (might be due to invalid IL or missing references)
		//IL_0906: Unknown result type (might be due to invalid IL or missing references)
		//IL_090d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_2bb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_2c00: Unknown result type (might be due to invalid IL or missing references)
		//IL_2c4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2c9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2cef: Unknown result type (might be due to invalid IL or missing references)
		//IL_2d22: Unknown result type (might be due to invalid IL or missing references)
		//IL_271d: Unknown result type (might be due to invalid IL or missing references)
		//IL_26d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_18e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1909: Unknown result type (might be due to invalid IL or missing references)
		//IL_1932: Unknown result type (might be due to invalid IL or missing references)
		//IL_1952: Unknown result type (might be due to invalid IL or missing references)
		//IL_1961: Unknown result type (might be due to invalid IL or missing references)
		//IL_1974: Unknown result type (might be due to invalid IL or missing references)
		//IL_1979: Unknown result type (might be due to invalid IL or missing references)
		//IL_197a: Unknown result type (might be due to invalid IL or missing references)
		//IL_198c: Unknown result type (might be due to invalid IL or missing references)
		//IL_198e: Unknown result type (might be due to invalid IL or missing references)
		//IL_198f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1996: Unknown result type (might be due to invalid IL or missing references)
		//IL_19ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_19de: Unknown result type (might be due to invalid IL or missing references)
		//IL_19e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_19e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_19f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_19f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_19fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a01: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a22: Unknown result type (might be due to invalid IL or missing references)
		//IL_125e: Unknown result type (might be due to invalid IL or missing references)
		//IL_092c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a15: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0636: Unknown result type (might be due to invalid IL or missing references)
		//IL_0669: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ecd: Unknown result type (might be due to invalid IL or missing references)
		//IL_2ef4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2f4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2f9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_2fea: Unknown result type (might be due to invalid IL or missing references)
		//IL_301a: Unknown result type (might be due to invalid IL or missing references)
		//IL_3041: Unknown result type (might be due to invalid IL or missing references)
		//IL_3074: Unknown result type (might be due to invalid IL or missing references)
		//IL_2d89: Unknown result type (might be due to invalid IL or missing references)
		//IL_2d59: Unknown result type (might be due to invalid IL or missing references)
		//IL_2768: Unknown result type (might be due to invalid IL or missing references)
		//IL_282d: Unknown result type (might be due to invalid IL or missing references)
		//IL_12da: Unknown result type (might be due to invalid IL or missing references)
		//IL_132e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3134: Unknown result type (might be due to invalid IL or missing references)
		//IL_315e: Unknown result type (might be due to invalid IL or missing references)
		//IL_31a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_31f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_30c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_2dd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1791: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0969: Unknown result type (might be due to invalid IL or missing references)
		//IL_096e: Unknown result type (might be due to invalid IL or missing references)
		//IL_096f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		//IL_0984: Unknown result type (might be due to invalid IL or missing references)
		//IL_0985: Unknown result type (might be due to invalid IL or missing references)
		//IL_098c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08be: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_08da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d43: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d51: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b72: Unknown result type (might be due to invalid IL or missing references)
		//IL_3bb6: Unknown result type (might be due to invalid IL or missing references)
		//IL_3c06: Unknown result type (might be due to invalid IL or missing references)
		//IL_3c53: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ca3: Unknown result type (might be due to invalid IL or missing references)
		//IL_3cf3: Unknown result type (might be due to invalid IL or missing references)
		//IL_3d23: Unknown result type (might be due to invalid IL or missing references)
		//IL_3d4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_3d74: Unknown result type (might be due to invalid IL or missing references)
		//IL_3da1: Unknown result type (might be due to invalid IL or missing references)
		//IL_3dce: Unknown result type (might be due to invalid IL or missing references)
		//IL_32ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_3303: Unknown result type (might be due to invalid IL or missing references)
		//IL_3328: Unknown result type (might be due to invalid IL or missing references)
		//IL_3218: Unknown result type (might be due to invalid IL or missing references)
		//IL_325d: Unknown result type (might be due to invalid IL or missing references)
		//IL_328e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2e3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_28b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1aa9: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ad2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1aee: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1afc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b01: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b30: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b35: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b39: Unknown result type (might be due to invalid IL or missing references)
		//IL_1872: Unknown result type (might be due to invalid IL or missing references)
		//IL_1424: Unknown result type (might be due to invalid IL or missing references)
		//IL_1479: Unknown result type (might be due to invalid IL or missing references)
		//IL_0860: Unknown result type (might be due to invalid IL or missing references)
		//IL_0887: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3f64: Unknown result type (might be due to invalid IL or missing references)
		//IL_3fb7: Unknown result type (might be due to invalid IL or missing references)
		//IL_4007: Unknown result type (might be due to invalid IL or missing references)
		//IL_4057: Unknown result type (might be due to invalid IL or missing references)
		//IL_40a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_40f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_4149: Unknown result type (might be due to invalid IL or missing references)
		//IL_418b: Unknown result type (might be due to invalid IL or missing references)
		//IL_41c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_4208: Unknown result type (might be due to invalid IL or missing references)
		//IL_4232: Unknown result type (might be due to invalid IL or missing references)
		//IL_33f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_341c: Unknown result type (might be due to invalid IL or missing references)
		//IL_345d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3471: Unknown result type (might be due to invalid IL or missing references)
		//IL_347b: Unknown result type (might be due to invalid IL or missing references)
		//IL_3480: Unknown result type (might be due to invalid IL or missing references)
		//IL_3489: Unknown result type (might be due to invalid IL or missing references)
		//IL_348e: Unknown result type (might be due to invalid IL or missing references)
		//IL_34ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_34cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_34d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_2e84: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b85: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ba0: Unknown result type (might be due to invalid IL or missing references)
		//IL_14f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b60: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b78: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_45a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_45e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_4637: Unknown result type (might be due to invalid IL or missing references)
		//IL_4686: Unknown result type (might be due to invalid IL or missing references)
		//IL_46d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_46ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_4713: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e38: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_3e73: Unknown result type (might be due to invalid IL or missing references)
		//IL_3545: Unknown result type (might be due to invalid IL or missing references)
		//IL_3564: Unknown result type (might be due to invalid IL or missing references)
		//IL_3585: Unknown result type (might be due to invalid IL or missing references)
		//IL_35ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_35ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_35e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_35fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_3600: Unknown result type (might be due to invalid IL or missing references)
		//IL_3601: Unknown result type (might be due to invalid IL or missing references)
		//IL_3613: Unknown result type (might be due to invalid IL or missing references)
		//IL_3615: Unknown result type (might be due to invalid IL or missing references)
		//IL_3616: Unknown result type (might be due to invalid IL or missing references)
		//IL_361d: Unknown result type (might be due to invalid IL or missing references)
		//IL_362e: Unknown result type (might be due to invalid IL or missing references)
		//IL_3653: Unknown result type (might be due to invalid IL or missing references)
		//IL_3678: Unknown result type (might be due to invalid IL or missing references)
		//IL_3697: Unknown result type (might be due to invalid IL or missing references)
		//IL_36b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_36de: Unknown result type (might be due to invalid IL or missing references)
		//IL_34de: Unknown result type (might be due to invalid IL or missing references)
		//IL_1570: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b42: Unknown result type (might be due to invalid IL or missing references)
		//IL_48e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_475b: Unknown result type (might be due to invalid IL or missing references)
		//IL_42ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_42d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ed3: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ac2: Unknown result type (might be due to invalid IL or missing references)
		//IL_3ae4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2948: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ddd: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e53: Unknown result type (might be due to invalid IL or missing references)
		//IL_4e82: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ebb: Unknown result type (might be due to invalid IL or missing references)
		//IL_47a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_4312: Unknown result type (might be due to invalid IL or missing references)
		//IL_3b28: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bcf: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bd4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1be7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1be9: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bea: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c07: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c55: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c86: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_4f6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4f07: Unknown result type (might be due to invalid IL or missing references)
		//IL_4d0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_4d4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_4d84: Unknown result type (might be due to invalid IL or missing references)
		//IL_47eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_4365: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f85: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f94: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fa2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fda: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cb7: Unknown result type (might be due to invalid IL or missing references)
		//IL_50d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_50fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_4fb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_481d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4861: Unknown result type (might be due to invalid IL or missing references)
		//IL_4894: Unknown result type (might be due to invalid IL or missing references)
		//IL_43b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_2052: Unknown result type (might be due to invalid IL or missing references)
		//IL_2057: Unknown result type (might be due to invalid IL or missing references)
		//IL_2058: Unknown result type (might be due to invalid IL or missing references)
		//IL_206a: Unknown result type (might be due to invalid IL or missing references)
		//IL_206c: Unknown result type (might be due to invalid IL or missing references)
		//IL_206d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2074: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fee: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ff3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ff4: Unknown result type (might be due to invalid IL or missing references)
		//IL_2006: Unknown result type (might be due to invalid IL or missing references)
		//IL_2008: Unknown result type (might be due to invalid IL or missing references)
		//IL_2009: Unknown result type (might be due to invalid IL or missing references)
		//IL_2010: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dab: Unknown result type (might be due to invalid IL or missing references)
		//IL_1db0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1db1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dc3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dc6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dcd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dec: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e05: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e20: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e21: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e28: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e89: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ed0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f10: Unknown result type (might be due to invalid IL or missing references)
		//IL_52f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_5337: Unknown result type (might be due to invalid IL or missing references)
		//IL_5370: Unknown result type (might be due to invalid IL or missing references)
		//IL_502b: Unknown result type (might be due to invalid IL or missing references)
		//IL_4451: Unknown result type (might be due to invalid IL or missing references)
		//IL_4409: Unknown result type (might be due to invalid IL or missing references)
		//IL_3794: Unknown result type (might be due to invalid IL or missing references)
		//IL_29e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_21be: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_21cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_21d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_21db: Unknown result type (might be due to invalid IL or missing references)
		//IL_2209: Unknown result type (might be due to invalid IL or missing references)
		//IL_220e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2212: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d42: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d47: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d48: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d64: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d84: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d98: Unknown result type (might be due to invalid IL or missing references)
		//IL_53be: Unknown result type (might be due to invalid IL or missing references)
		//IL_5170: Unknown result type (might be due to invalid IL or missing references)
		//IL_519e: Unknown result type (might be due to invalid IL or missing references)
		//IL_51d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_5077: Unknown result type (might be due to invalid IL or missing references)
		//IL_2a6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_20e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_228a: Unknown result type (might be due to invalid IL or missing references)
		//IL_228f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2290: Unknown result type (might be due to invalid IL or missing references)
		//IL_22a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_22a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_22a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_22ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_2226: Unknown result type (might be due to invalid IL or missing references)
		//IL_222b: Unknown result type (might be due to invalid IL or missing references)
		//IL_222c: Unknown result type (might be due to invalid IL or missing references)
		//IL_223e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2240: Unknown result type (might be due to invalid IL or missing references)
		//IL_2241: Unknown result type (might be due to invalid IL or missing references)
		//IL_2248: Unknown result type (might be due to invalid IL or missing references)
		//IL_55c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_5608: Unknown result type (might be due to invalid IL or missing references)
		//IL_563e: Unknown result type (might be due to invalid IL or missing references)
		//IL_5673: Unknown result type (might be due to invalid IL or missing references)
		//IL_56aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_56d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_5564: Unknown result type (might be due to invalid IL or missing references)
		//IL_49d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_455b: Unknown result type (might be due to invalid IL or missing references)
		//IL_451c: Unknown result type (might be due to invalid IL or missing references)
		//IL_44b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_2b22: Unknown result type (might be due to invalid IL or missing references)
		//IL_2109: Unknown result type (might be due to invalid IL or missing references)
		//IL_2118: Unknown result type (might be due to invalid IL or missing references)
		//IL_211d: Unknown result type (might be due to invalid IL or missing references)
		//IL_211e: Unknown result type (might be due to invalid IL or missing references)
		//IL_2131: Unknown result type (might be due to invalid IL or missing references)
		//IL_2133: Unknown result type (might be due to invalid IL or missing references)
		//IL_2134: Unknown result type (might be due to invalid IL or missing references)
		//IL_213b: Unknown result type (might be due to invalid IL or missing references)
		//IL_218c: Unknown result type (might be due to invalid IL or missing references)
		//IL_208f: Unknown result type (might be due to invalid IL or missing references)
		//IL_20a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_20a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_20a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_20ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_20bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_20bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_20c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_5876: Unknown result type (might be due to invalid IL or missing references)
		//IL_58b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_54b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_521f: Unknown result type (might be due to invalid IL or missing references)
		//IL_231e: Unknown result type (might be due to invalid IL or missing references)
		//IL_591b: Unknown result type (might be due to invalid IL or missing references)
		//IL_595c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5748: Unknown result type (might be due to invalid IL or missing references)
		//IL_5776: Unknown result type (might be due to invalid IL or missing references)
		//IL_57af: Unknown result type (might be due to invalid IL or missing references)
		//IL_54cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_54d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_54d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_54df: Invalid comparison between Unknown and I4
		//IL_542d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5432: Unknown result type (might be due to invalid IL or missing references)
		//IL_5442: Unknown result type (might be due to invalid IL or missing references)
		//IL_5447: Unknown result type (might be due to invalid IL or missing references)
		//IL_544d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5453: Invalid comparison between Unknown and I4
		//IL_5290: Unknown result type (might be due to invalid IL or missing references)
		//IL_525a: Unknown result type (might be due to invalid IL or missing references)
		//IL_2340: Unknown result type (might be due to invalid IL or missing references)
		//IL_234f: Unknown result type (might be due to invalid IL or missing references)
		//IL_2354: Unknown result type (might be due to invalid IL or missing references)
		//IL_2355: Unknown result type (might be due to invalid IL or missing references)
		//IL_2368: Unknown result type (might be due to invalid IL or missing references)
		//IL_236a: Unknown result type (might be due to invalid IL or missing references)
		//IL_236b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2372: Unknown result type (might be due to invalid IL or missing references)
		//IL_23c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_22c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_22da: Unknown result type (might be due to invalid IL or missing references)
		//IL_22df: Unknown result type (might be due to invalid IL or missing references)
		//IL_22e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_22f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_22f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_22f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_22fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_57f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_5459: Unknown result type (might be due to invalid IL or missing references)
		//IL_5460: Invalid comparison between Unknown and I4
		//IL_387a: Unknown result type (might be due to invalid IL or missing references)
		//IL_388d: Unknown result type (might be due to invalid IL or missing references)
		//IL_3892: Unknown result type (might be due to invalid IL or missing references)
		//IL_3893: Unknown result type (might be due to invalid IL or missing references)
		//IL_38a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_38a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_38a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_38af: Unknown result type (might be due to invalid IL or missing references)
		//IL_38ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_38f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_391f: Unknown result type (might be due to invalid IL or missing references)
		//IL_3949: Unknown result type (might be due to invalid IL or missing references)
		//IL_3962: Unknown result type (might be due to invalid IL or missing references)
		//IL_3967: Unknown result type (might be due to invalid IL or missing references)
		//IL_3968: Unknown result type (might be due to invalid IL or missing references)
		//IL_397b: Unknown result type (might be due to invalid IL or missing references)
		//IL_397d: Unknown result type (might be due to invalid IL or missing references)
		//IL_397e: Unknown result type (might be due to invalid IL or missing references)
		//IL_3985: Unknown result type (might be due to invalid IL or missing references)
		//IL_3811: Unknown result type (might be due to invalid IL or missing references)
		//IL_3824: Unknown result type (might be due to invalid IL or missing references)
		//IL_3829: Unknown result type (might be due to invalid IL or missing references)
		//IL_382a: Unknown result type (might be due to invalid IL or missing references)
		//IL_383c: Unknown result type (might be due to invalid IL or missing references)
		//IL_383e: Unknown result type (might be due to invalid IL or missing references)
		//IL_383f: Unknown result type (might be due to invalid IL or missing references)
		//IL_3846: Unknown result type (might be due to invalid IL or missing references)
		//IL_3866: Unknown result type (might be due to invalid IL or missing references)
		//IL_59bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_5a00: Unknown result type (might be due to invalid IL or missing references)
		//IL_5a16: Unknown result type (might be due to invalid IL or missing references)
		//IL_5a3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_5a79: Unknown result type (might be due to invalid IL or missing references)
		//IL_5abc: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ae3: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a73: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a82: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a90: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ac5: Unknown result type (might be due to invalid IL or missing references)
		//IL_4aca: Unknown result type (might be due to invalid IL or missing references)
		//IL_4ace: Unknown result type (might be due to invalid IL or missing references)
		//IL_4a5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_39c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_39cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_39d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_39dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_39e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a10: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a38: Unknown result type (might be due to invalid IL or missing references)
		//IL_3a84: Unknown result type (might be due to invalid IL or missing references)
		//IL_39b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_5b70: Unknown result type (might be due to invalid IL or missing references)
		//IL_5bc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c75: Unknown result type (might be due to invalid IL or missing references)
		//IL_5c9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_4c46: Unknown result type (might be due to invalid IL or missing references)
		//IL_4af8: Unknown result type (might be due to invalid IL or missing references)
		//IL_4b7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_4bd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_4c09: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ea9: Unknown result type (might be due to invalid IL or missing references)
		//IL_5ef3: Unknown result type (might be due to invalid IL or missing references)
		//IL_5f42: Unknown result type (might be due to invalid IL or missing references)
		//IL_5f92: Unknown result type (might be due to invalid IL or missing references)
		//IL_5fc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_6021: Unknown result type (might be due to invalid IL or missing references)
		//IL_6059: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d00: Unknown result type (might be due to invalid IL or missing references)
		//IL_4c62: Unknown result type (might be due to invalid IL or missing references)
		//IL_60be: Unknown result type (might be due to invalid IL or missing references)
		//IL_6108: Unknown result type (might be due to invalid IL or missing references)
		//IL_615b: Unknown result type (might be due to invalid IL or missing references)
		//IL_61ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_61fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_624a: Unknown result type (might be due to invalid IL or missing references)
		//IL_629a: Unknown result type (might be due to invalid IL or missing references)
		//IL_62cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_631f: Unknown result type (might be due to invalid IL or missing references)
		//IL_633b: Unknown result type (might be due to invalid IL or missing references)
		//IL_6362: Unknown result type (might be due to invalid IL or missing references)
		//IL_5d63: Unknown result type (might be due to invalid IL or missing references)
		//IL_65e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_661e: Unknown result type (might be due to invalid IL or missing references)
		//IL_665a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6696: Unknown result type (might be due to invalid IL or missing references)
		//IL_66d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_670e: Unknown result type (might be due to invalid IL or missing references)
		//IL_6749: Unknown result type (might be due to invalid IL or missing references)
		//IL_6785: Unknown result type (might be due to invalid IL or missing references)
		//IL_67c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_67eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_682f: Unknown result type (might be due to invalid IL or missing references)
		//IL_6845: Unknown result type (might be due to invalid IL or missing references)
		//IL_686c: Unknown result type (might be due to invalid IL or missing references)
		//IL_63aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_5e3f: Unknown result type (might be due to invalid IL or missing references)
		//IL_68d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_6915: Unknown result type (might be due to invalid IL or missing references)
		//IL_6936: Unknown result type (might be due to invalid IL or missing references)
		//IL_695d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6999: Unknown result type (might be due to invalid IL or missing references)
		//IL_69d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_640d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6a98: Unknown result type (might be due to invalid IL or missing references)
		//IL_6aec: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b30: Unknown result type (might be due to invalid IL or missing references)
		//IL_6b57: Unknown result type (might be due to invalid IL or missing references)
		//IL_6482: Unknown result type (might be due to invalid IL or missing references)
		//IL_6451: Unknown result type (might be due to invalid IL or missing references)
		//IL_6bfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_6c2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_6c66: Unknown result type (might be due to invalid IL or missing references)
		//IL_6c8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ca9: Unknown result type (might be due to invalid IL or missing references)
		//IL_6cd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_6d09: Unknown result type (might be due to invalid IL or missing references)
		//IL_6d3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6d5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_6d8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_8369: Unknown result type (might be due to invalid IL or missing references)
		//IL_839d: Unknown result type (might be due to invalid IL or missing references)
		//IL_83d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_83f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_841f: Unknown result type (might be due to invalid IL or missing references)
		//IL_8453: Unknown result type (might be due to invalid IL or missing references)
		//IL_8472: Unknown result type (might be due to invalid IL or missing references)
		//IL_849e: Unknown result type (might be due to invalid IL or missing references)
		//IL_84d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_8502: Unknown result type (might be due to invalid IL or missing references)
		//IL_8539: Unknown result type (might be due to invalid IL or missing references)
		//IL_8564: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ee3: Unknown result type (might be due to invalid IL or missing references)
		//IL_6f1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6dd8: Unknown result type (might be due to invalid IL or missing references)
		//IL_64e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_7f9d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6f41: Unknown result type (might be due to invalid IL or missing references)
		//IL_6dff: Unknown result type (might be due to invalid IL or missing references)
		//IL_6e2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_655a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6529: Unknown result type (might be due to invalid IL or missing references)
		//IL_8586: Unknown result type (might be due to invalid IL or missing references)
		//IL_858b: Unknown result type (might be due to invalid IL or missing references)
		//IL_8594: Unknown result type (might be due to invalid IL or missing references)
		//IL_8599: Unknown result type (might be due to invalid IL or missing references)
		//IL_85bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_85c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_85c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_8698: Unknown result type (might be due to invalid IL or missing references)
		//IL_869d: Unknown result type (might be due to invalid IL or missing references)
		//IL_869e: Unknown result type (might be due to invalid IL or missing references)
		//IL_86b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_86b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_86b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_86ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_86bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_86c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_86cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_86d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_8711: Unknown result type (might be due to invalid IL or missing references)
		//IL_8716: Unknown result type (might be due to invalid IL or missing references)
		//IL_871a: Unknown result type (might be due to invalid IL or missing references)
		//IL_6f6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6f88: Unknown result type (might be due to invalid IL or missing references)
		//IL_6fa8: Unknown result type (might be due to invalid IL or missing references)
		//IL_6e6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_6e9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_862d: Unknown result type (might be due to invalid IL or missing references)
		//IL_865f: Unknown result type (might be due to invalid IL or missing references)
		//IL_85f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_87c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_87e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_87e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_87e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_87fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_87fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_87fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_8804: Unknown result type (might be due to invalid IL or missing references)
		//IL_8839: Unknown result type (might be due to invalid IL or missing references)
		//IL_872a: Unknown result type (might be due to invalid IL or missing references)
		//IL_872f: Unknown result type (might be due to invalid IL or missing references)
		//IL_8730: Unknown result type (might be due to invalid IL or missing references)
		//IL_8743: Unknown result type (might be due to invalid IL or missing references)
		//IL_8745: Unknown result type (might be due to invalid IL or missing references)
		//IL_8746: Unknown result type (might be due to invalid IL or missing references)
		//IL_874d: Unknown result type (might be due to invalid IL or missing references)
		//IL_877c: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fae: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fb3: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fbc: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fc1: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fe7: Unknown result type (might be due to invalid IL or missing references)
		//IL_7fec: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ff0: Unknown result type (might be due to invalid IL or missing references)
		//IL_80c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_80c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_80c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_80d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_80da: Unknown result type (might be due to invalid IL or missing references)
		//IL_80db: Unknown result type (might be due to invalid IL or missing references)
		//IL_80e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_80e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_80ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_80f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_80f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_8139: Unknown result type (might be due to invalid IL or missing references)
		//IL_813e: Unknown result type (might be due to invalid IL or missing references)
		//IL_8142: Unknown result type (might be due to invalid IL or missing references)
		//IL_77f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_780b: Unknown result type (might be due to invalid IL or missing references)
		//IL_782b: Unknown result type (might be due to invalid IL or missing references)
		//IL_784b: Unknown result type (might be due to invalid IL or missing references)
		//IL_701d: Unknown result type (might be due to invalid IL or missing references)
		//IL_6ff8: Unknown result type (might be due to invalid IL or missing references)
		//IL_8055: Unknown result type (might be due to invalid IL or missing references)
		//IL_8087: Unknown result type (might be due to invalid IL or missing references)
		//IL_801a: Unknown result type (might be due to invalid IL or missing references)
		//IL_81f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_8209: Unknown result type (might be due to invalid IL or missing references)
		//IL_820e: Unknown result type (might be due to invalid IL or missing references)
		//IL_820f: Unknown result type (might be due to invalid IL or missing references)
		//IL_8222: Unknown result type (might be due to invalid IL or missing references)
		//IL_8224: Unknown result type (might be due to invalid IL or missing references)
		//IL_8225: Unknown result type (might be due to invalid IL or missing references)
		//IL_822c: Unknown result type (might be due to invalid IL or missing references)
		//IL_8261: Unknown result type (might be due to invalid IL or missing references)
		//IL_8152: Unknown result type (might be due to invalid IL or missing references)
		//IL_8157: Unknown result type (might be due to invalid IL or missing references)
		//IL_8158: Unknown result type (might be due to invalid IL or missing references)
		//IL_816b: Unknown result type (might be due to invalid IL or missing references)
		//IL_816d: Unknown result type (might be due to invalid IL or missing references)
		//IL_816e: Unknown result type (might be due to invalid IL or missing references)
		//IL_8175: Unknown result type (might be due to invalid IL or missing references)
		//IL_81a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_78d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_789b: Unknown result type (might be due to invalid IL or missing references)
		//IL_703d: Unknown result type (might be due to invalid IL or missing references)
		//IL_704c: Unknown result type (might be due to invalid IL or missing references)
		//IL_78ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_70c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_70c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_70d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_7d58: Unknown result type (might be due to invalid IL or missing references)
		//IL_7d73: Unknown result type (might be due to invalid IL or missing references)
		//IL_7d93: Unknown result type (might be due to invalid IL or missing references)
		//IL_7da2: Unknown result type (might be due to invalid IL or missing references)
		//IL_729f: Unknown result type (might be due to invalid IL or missing references)
		//IL_7909: Unknown result type (might be due to invalid IL or missing references)
		//IL_790e: Unknown result type (might be due to invalid IL or missing references)
		//IL_790f: Unknown result type (might be due to invalid IL or missing references)
		//IL_7921: Unknown result type (might be due to invalid IL or missing references)
		//IL_7923: Unknown result type (might be due to invalid IL or missing references)
		//IL_7924: Unknown result type (might be due to invalid IL or missing references)
		//IL_792b: Unknown result type (might be due to invalid IL or missing references)
		//IL_70ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_70f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_70f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_7105: Unknown result type (might be due to invalid IL or missing references)
		//IL_7107: Unknown result type (might be due to invalid IL or missing references)
		//IL_7108: Unknown result type (might be due to invalid IL or missing references)
		//IL_710f: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e01: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e06: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e07: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e19: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e1b: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e23: Unknown result type (might be due to invalid IL or missing references)
		//IL_79d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_7977: Unknown result type (might be due to invalid IL or missing references)
		//IL_7218: Unknown result type (might be due to invalid IL or missing references)
		//IL_718a: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_7e4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a41: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a46: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a47: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_7a63: Unknown result type (might be due to invalid IL or missing references)
		//IL_72b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_72be: Unknown result type (might be due to invalid IL or missing references)
		//IL_72bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_72d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_72d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_72d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_72db: Unknown result type (might be due to invalid IL or missing references)
		//IL_7edb: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ee0: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ee1: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ef3: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ef5: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ef6: Unknown result type (might be due to invalid IL or missing references)
		//IL_7efd: Unknown result type (might be due to invalid IL or missing references)
		//IL_7b1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ab4: Unknown result type (might be due to invalid IL or missing references)
		//IL_7387: Unknown result type (might be due to invalid IL or missing references)
		//IL_7327: Unknown result type (might be due to invalid IL or missing references)
		//IL_7f63: Unknown result type (might be due to invalid IL or missing references)
		//IL_7f26: Unknown result type (might be due to invalid IL or missing references)
		//IL_7b8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_7b91: Unknown result type (might be due to invalid IL or missing references)
		//IL_7b92: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ba4: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ba6: Unknown result type (might be due to invalid IL or missing references)
		//IL_7ba7: Unknown result type (might be due to invalid IL or missing references)
		//IL_7bae: Unknown result type (might be due to invalid IL or missing references)
		//IL_7c65: Unknown result type (might be due to invalid IL or missing references)
		//IL_7c01: Unknown result type (might be due to invalid IL or missing references)
		//IL_7575: Unknown result type (might be due to invalid IL or missing references)
		//IL_757a: Unknown result type (might be due to invalid IL or missing references)
		//IL_757b: Unknown result type (might be due to invalid IL or missing references)
		//IL_758d: Unknown result type (might be due to invalid IL or missing references)
		//IL_758f: Unknown result type (might be due to invalid IL or missing references)
		//IL_7590: Unknown result type (might be due to invalid IL or missing references)
		//IL_7597: Unknown result type (might be due to invalid IL or missing references)
		//IL_7420: Unknown result type (might be due to invalid IL or missing references)
		//IL_7425: Unknown result type (might be due to invalid IL or missing references)
		//IL_7426: Unknown result type (might be due to invalid IL or missing references)
		//IL_7438: Unknown result type (might be due to invalid IL or missing references)
		//IL_743a: Unknown result type (might be due to invalid IL or missing references)
		//IL_743b: Unknown result type (might be due to invalid IL or missing references)
		//IL_7442: Unknown result type (might be due to invalid IL or missing references)
		//IL_7643: Unknown result type (might be due to invalid IL or missing references)
		//IL_75e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_74ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_7497: Unknown result type (might be due to invalid IL or missing references)
		//IL_76ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_76b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_76b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_76c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_76c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_76c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_76ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_777f: Unknown result type (might be due to invalid IL or missing references)
		//IL_771e: Unknown result type (might be due to invalid IL or missing references)
		GUI.skin = guiskin;
		Transform val = null;
		int num = 0;
		int num2 = 0;
		Vector3 val2 = default(Vector3);
		float num3 = default(float);
		float num4 = default(float);
		int num5 = 0;
		int num6 = 0;
		AudioListener.volume = audiovol;
		if (gamestate != 3 || gamestate != 6)
		{
			skymaterial.SetFloat("_Blend", 0f);
		}
		sharedhud = 0;
		int num7 = (int)((float)menulogo.width * 0.3f + 20f + ((float)Screen.width - ((float)menulogo.width * 0.3f + 20f)) / 2f);
		if (gamestate == -1)
		{
			gamestate = ZbMenu.EntryState(this); // phase 3: invite link (?room=CODE) or the normal main menu
		}
		ZbMenu.Gate(this); // phase 3: multiplayer entry screens ask for a nickname first
		if (gamestate == 29)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 25), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 25), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 20), 400f, 32f), language[languageset].frase[110]))
			{
				for (num = 2; num < 10; num++)
				{
					invent[num] = -1;
				}
				money = 0;
				invent[0] = 0;
				invent[1] = 8;
				my_beard = -1;
				my_beardcolor = 2;
				my_shirtcolor = 0;
				my_skincolor = 0;
				save();
				update_manequim();
				gamestate = 0;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 30), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 19;
			}
			GUI.color = Color.yellow;
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 70), 400f, 32f), "RESET");
		}
		if (gamestate == 19)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 150), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 145), 400f, 32f), language[languageset].frase[77]))
			{
				gamestate = 21;
				sortby_type_price();
				shopselected = itemtosell[shoparray[0]];
				shoptype = 0;
				shoparrayselected = 0;
				update_gunshow();
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[79]))
			{
				gamestate = 22;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[80]))
			{
				gamestate = 23;
				onmouse = -1;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), "RESET"))
			{
				gamestate = 29;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
			}
			GUI.color = Color.grey;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 95), 400f, 32f), language[languageset].frase[78]);
		}
		if (gamestate == 23)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.color = Color.yellow;
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 170), 400f, 32f), language[languageset].frase[60]);
			for (num = 0; num < 5; num++)
			{
				GUI.color = Color.white;
				val2 = Input.mousePosition;
				val2.y = (float)Screen.height - Input.mousePosition.y;
				Rect val3 = new Rect((float)(num7 - 110), (float)(Screen.height / 2 - 120 + 35 * num), 96f, 33f);
				if (val3.Contains(val2))
				{
					float a = 0.2f;
					Color color = GUI.color;
					float num8 = (color.a = a);
					Color val4 = (GUI.color = color);
					if (Input.GetKeyDown((KeyCode)323))
					{
						onmouse = num;
					}
					if (onmouse >= 0 && Input.GetKeyUp((KeyCode)323) && onmouse != num)
					{
						num5 = invent[onmouse];
						invent[onmouse] = invent[num];
						invent[num] = num5;
					}
					if (onmouse < 0 && invent[num] >= 0)
					{
						GUI.color = Color.yellow;
						GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 65), 400f, 32f), itemlist[invent[num]].nome);
					}
					GUI.color = Color.white;
					float a2 = 0.2f;
					Color color2 = GUI.color;
					float num9 = (color2.a = a2);
					Color val6 = (GUI.color = color2);
				}
				else
				{
					float a3 = 0.05f;
					Color color3 = GUI.color;
					float num10 = (color3.a = a3);
					Color val8 = (GUI.color = color3);
				}
				GUI.DrawTexture(new Rect((float)(num7 - 110), (float)(Screen.height / 2 - 120 + 35 * num), 96f, 33f), SL_line, (ScaleMode)0, true, 0f);
				if (invent[num] >= 0 && onmouse != num)
				{
					GUI.color = Color.white;
					int num11 = 1;
					Color color4 = GUI.color;
					float num12 = (color4.a = num11);
					Color val10 = (GUI.color = color4);
					GUI.DrawTexture(new Rect((float)(num7 - 110), (float)(Screen.height / 2 - 120 + 35 * num), (float)itemlist[invent[num]].sprite.width, (float)itemlist[invent[num]].sprite.height), itemlist[invent[num]].sprite, (ScaleMode)0, true, 0f);
				}
			}
			for (; num < 10; num++)
			{
				GUI.color = Color.white;
				val2 = Input.mousePosition;
				val2.y = (float)Screen.height - Input.mousePosition.y;
				Rect val12 = new Rect((float)(num7 + 5), (float)(Screen.height / 2 - 120 + 35 * (num - 5)), 96f, 33f);
				if (val12.Contains(val2))
				{
					float a4 = 0.2f;
					Color color5 = GUI.color;
					float num13 = (color5.a = a4);
					Color val13 = (GUI.color = color5);
					if (Input.GetKeyDown((KeyCode)323))
					{
						onmouse = num;
					}
					if (onmouse >= 0 && Input.GetKeyUp((KeyCode)323) && onmouse != num)
					{
						num5 = invent[onmouse];
						invent[onmouse] = invent[num];
						invent[num] = num5;
					}
					if (onmouse < 0 && invent[num] >= 0)
					{
						GUI.color = Color.yellow;
						GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 65), 400f, 32f), itemlist[invent[num]].nome);
					}
					GUI.color = Color.white;
					float a5 = 0.2f;
					Color color6 = GUI.color;
					float num14 = (color6.a = a5);
					Color val15 = (GUI.color = color6);
				}
				else
				{
					float a6 = 0.05f;
					Color color7 = GUI.color;
					float num15 = (color7.a = a6);
					Color val17 = (GUI.color = color7);
				}
				GUI.DrawTexture(new Rect((float)(num7 + 5), (float)(Screen.height / 2 - 120 + 35 * (num - 5)), 96f, 33f), SL_line, (ScaleMode)0, true, 0f);
				if (invent[num] >= 0 && onmouse != num)
				{
					GUI.color = Color.white;
					int num16 = 1;
					Color color8 = GUI.color;
					float num17 = (color8.a = num16);
					Color val19 = (GUI.color = color8);
					GUI.DrawTexture(new Rect((float)(num7 + 5), (float)(Screen.height / 2 - 120 + 35 * (num - 5)), (float)itemlist[invent[num]].sprite.width, (float)itemlist[invent[num]].sprite.height), itemlist[invent[num]].sprite, (ScaleMode)0, true, 0f);
				}
			}
			if (Input.GetKeyUp((KeyCode)323) && onmouse != num)
			{
				onmouse = -1;
			}
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				save();
				gamestate = 19;
			}
			val2 = Input.mousePosition;
			val2.y = (float)Screen.height - Input.mousePosition.y;
			if (onmouse >= 0 && invent[onmouse] >= 0)
			{
				GUI.DrawTexture(new Rect(val2.x - 48f, val2.y - 16f, (float)itemlist[invent[onmouse]].sprite.width, (float)itemlist[invent[onmouse]].sprite.height), itemlist[invent[onmouse]].sprite, (ScaleMode)0, true, 0f);
			}
		}
		if (gamestate == 22)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			playernick = GUI.TextField(new Rect((float)(num7 - 50), (float)(Screen.height / 2 - 142), 200f, 30f), playernick, 10);
			playernick = ZbMenu.FilterNick(playernick); // phase 3: letters, digits, space _ - . only
			GUI.Label(new Rect((float)(num7 - 190), (float)(Screen.height / 2 - 145), 175f, 32f), language[languageset].frase[96]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 95), 400f, 32f), language[languageset].frase[94] + " #" + (my_skincolor + 1));
			if (GUI.Button(new Rect((float)(num7 + 160), (float)(Screen.height / 2 - 95), 30f, 32f), ">"))
			{
				my_skincolor++;
				if (my_skincolor > ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).skincolors.Length - 1)
				{
					my_skincolor = 0;
				}
				update_manequim();
			}
			if (GUI.Button(new Rect((float)(num7 - 190), (float)(Screen.height / 2 - 95), 30f, 32f), "<"))
			{
				my_skincolor--;
				if (my_skincolor < 0)
				{
					my_skincolor = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).skincolors.Length - 1;
				}
				update_manequim();
			}
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[90] + " #" + (my_beard + 2));
			if (GUI.Button(new Rect((float)(num7 + 160), (float)(Screen.height / 2 - 45), 30f, 32f), ">"))
			{
				my_beard++;
				if (my_beard > ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).beardprefabs.Length - 1)
				{
					my_beard = -1;
				}
				update_manequim();
			}
			if (GUI.Button(new Rect((float)(num7 - 190), (float)(Screen.height / 2 - 45), 30f, 32f), "<"))
			{
				my_beard--;
				if (my_beard < -1)
				{
					my_beard = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).beardprefabs.Length - 1;
				}
				update_manequim();
			}
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[91] + " #" + (my_beardcolor + 1));
			if (GUI.Button(new Rect((float)(num7 + 160), (float)(Screen.height / 2 + 5), 30f, 32f), ">"))
			{
				my_beardcolor++;
				if (my_beardcolor > ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).haircolors.Length - 1)
				{
					my_beardcolor = 0;
				}
				update_manequim();
			}
			if (GUI.Button(new Rect((float)(num7 - 190), (float)(Screen.height / 2 + 5), 30f, 32f), "<"))
			{
				my_beardcolor--;
				if (my_beardcolor < 0)
				{
					my_beardcolor = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).haircolors.Length - 1;
				}
				update_manequim();
			}
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), language[languageset].frase[92] + " #" + (my_shirtcolor + 1));
			if (GUI.Button(new Rect((float)(num7 + 160), (float)(Screen.height / 2 + 55), 30f, 32f), ">"))
			{
				my_shirtcolor++;
				if (my_shirtcolor > ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).camisacolors.Length - 1)
				{
					my_shirtcolor = 0;
				}
				update_manequim();
			}
			if (GUI.Button(new Rect((float)(num7 - 190), (float)(Screen.height / 2 + 55), 30f, 32f), "<"))
			{
				my_shirtcolor--;
				if (my_shirtcolor < 0)
				{
					my_shirtcolor = ((sc_player)(object)((Component)playerprefab).GetComponent(typeof(sc_player))).camisacolors.Length - 1;
				}
				update_manequim();
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				save();
				gamestate = 19;
			}
		}
		if (gamestate == 21)
		{
			sharedhud = 2;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.Box(new Rect(225f, 5f, (float)(Screen.width - 230), (float)(Screen.height - 250)), string.Empty);
			GUI.DrawTexture(new Rect((float)(num7 + 20), (float)(Screen.height - 200), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 + 20), (float)(Screen.height - 100), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.color = Color.gray;
			GUI.DrawTexture(new Rect((float)(num7 + 20), (float)(Screen.height - 150), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 + 22), (float)(Screen.height - 145), 200f, 32f), language[languageset].frase[89]);
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 + 22), (float)(Screen.height - 95), 200f, 32f), language[languageset].frase[3]))
			{
				gamestate = 19;
			}
			if (shopselected >= 0 && shoparrayselected >= 0)
			{
				if (shoptype == 0 && GUI.Button(new Rect((float)(num7 + 22), (float)(Screen.height - 195), 200f, 32f), language[languageset].frase[65]) && money >= itemlist[shopselected].price)
				{
					for (num = 0; num < invent.Length && invent[num] >= 0; num++)
					{
					}
					if (num < invent.Length)
					{
						money -= itemlist[shopselected].price;
						invent[num] = shopselected;
						save();
						update_gunshow();
					}
				}
				if (shoptype == 1 && GUI.Button(new Rect((float)(num7 + 22), (float)(Screen.height - 195), 200f, 32f), language[languageset].frase[64]))
				{
					money += itemlist[shopselected].sellprice;
					invent[shoparrayselected] = -1;
					save();
					shopselected = -1;
					update_gunshow();
					shoptype = -1;
				}
			}
			GUI.skin = newgui_square2;
			GUI.color = Color.red;
			GUI.Box(new Rect(225f, 5f, (float)(Screen.width - 230), 30f), string.Empty);
			GUI.Box(new Rect((float)(Screen.width - 25), 5f, 20f, (float)(Screen.height - 249)), string.Empty);
			GUI.Box(new Rect((float)(Screen.width - 25), 5f, 20f, 30f), string.Empty);
			GUI.color = Color.white;
			float a7 = 0.02f;
			Color color9 = GUI.color;
			float num18 = (color9.a = a7);
			Color val21 = (GUI.color = color9);
			GUI.DrawTexture(new Rect((float)(num7 - 260), (float)(Screen.height - 240), 250f, 230f), SL_line, (ScaleMode)0, true, 0f);
			GUI.skin = guiskinerr;
			int num19 = 1;
			Color color10 = GUI.color;
			float num20 = (color10.a = num19);
			Color val23 = (GUI.color = color10);
			GUI.Label(new Rect((float)(num7 - 255), (float)(Screen.height - 240), 250f, 30f), language[languageset].frase[60]);
			num5 = 0;
			for (num = 0; num + shopadicional < shoparray.Length && 79 + 40 * num < Screen.height - 240; num++)
			{
				num5++;
			}
			num5 = shoparray.Length - num5;
			if (shopadicional > num5)
			{
				shopadicional = num5;
			}
			GUI.color = Color.yellow;
			shopadicional = (int)GUI.VerticalSlider(new Rect((float)(Screen.width - 22), 37f, 20f, (float)(Screen.height - 249 - 40)), (float)shopadicional, 0f, (float)num5);
			val2 = Input.mousePosition;
			val2.y = (float)Screen.height - Input.mousePosition.y;
			Rect val25 = new Rect(225f, 5f, (float)(Screen.width - 230), (float)(Screen.height - 250));
			if (val25.Contains(val2))
			{
				shopadicional = (int)((float)shopadicional - Input.GetAxis("MouseWheel"));
				if (shopadicional > num5)
				{
					shopadicional = num5;
				}
				if (shopadicional < 0)
				{
					shopadicional = 0;
				}
			}
			GUI.color = Color.white;
			GUI.Label(new Rect(225f, 5f, 300f, 30f), language[languageset].frase[77]);
			for (num = 0; num + shopadicional < shoparray.Length && 79 + 40 * num < Screen.height - 240; num++)
			{
				float a8 = 0.2f;
				Color color11 = GUI.color;
				float num21 = (color11.a = a8);
				Color val26 = (GUI.color = color11);
				GUI.DrawTexture(new Rect(350f, (float)(33 + 40 * num), 2f, 37f), SL_line, (ScaleMode)0, true, 0f);
				GUI.DrawTexture(new Rect(550f, (float)(33 + 40 * num), 2f, 37f), SL_line, (ScaleMode)0, true, 0f);
				GUI.DrawTexture(new Rect(730f, (float)(33 + 40 * num), 2f, 37f), SL_line, (ScaleMode)0, true, 0f);
				Rect val28 = new Rect(225f, (float)(34 + 40 * num), (float)(Screen.width - 260), 35f);
				if (val28.Contains(val2))
				{
					GUI.DrawTexture(new Rect(228f, (float)(34 + 40 * num), (float)(Screen.width - 265), 35f), SL_line, (ScaleMode)0, true, 0f);
					if (Input.GetKeyDown((KeyCode)323))
					{
						shopselected = itemtosell[shoparray[num + shopadicional]];
						update_gunshow();
						shoptype = 0;
						shoparrayselected = num + shopadicional;
					}
				}
				if (num + shopadicional == shoparrayselected && shoptype == 0)
				{
					GUI.color = Color.yellow;
					float a9 = 0.2f;
					Color color12 = GUI.color;
					float num22 = (color12.a = a9);
					Color val29 = (GUI.color = color12);
					GUI.DrawTexture(new Rect(228f, (float)(34 + 40 * num), (float)(Screen.width - 265), 35f), SL_line, (ScaleMode)0, true, 0f);
					GUI.color = Color.white;
				}
				float a10 = 0.3f;
				Color color13 = GUI.color;
				float num23 = (color13.a = a10);
				Color val31 = (GUI.color = color13);
				GUI.DrawTexture(new Rect(230f, (float)(70 + 40 * num), (float)(Screen.width - 300), 2f), SL_line, (ScaleMode)0, true, 0f);
				int num24 = 1;
				Color color14 = GUI.color;
				float num25 = (color14.a = num24);
				Color val33 = (GUI.color = color14);
				GUI.DrawTexture(new Rect(240f, (float)(34 + 40 * num), (float)itemlist[itemtosell[shoparray[num + shopadicional]]].sprite.width, (float)itemlist[itemtosell[shoparray[num + shopadicional]]].sprite.height), itemlist[itemtosell[shoparray[num + shopadicional]]].sprite, (ScaleMode)0, true, 0f);
				GUI.Label(new Rect(350f, (float)(35 + 40 * num), 200f, 30f), itemlist[itemtosell[shoparray[num + shopadicional]]].nome);
				GUI.Label(new Rect(580f, (float)(35 + 40 * num), 100f, 30f), itemlist[itemtosell[shoparray[num + shopadicional]]].price + " $");
			}
			for (num = 0; num < 5; num++)
			{
				GUI.color = Color.white;
				val2 = Input.mousePosition;
				val2.y = (float)Screen.height - Input.mousePosition.y;
				Rect val35 = new Rect((float)(num7 - 250), (float)(Screen.height - 200 + 35 * num), 96f, 33f);
				if (val35.Contains(val2))
				{
					float a11 = 0.2f;
					Color color15 = GUI.color;
					float num26 = (color15.a = a11);
					Color val36 = (GUI.color = color15);
					if (Input.GetKeyDown((KeyCode)323))
					{
						shopselected = invent[num];
						shoparrayselected = num;
						update_gunshow();
						shoptype = 1;
					}
				}
				else
				{
					float a12 = 0.05f;
					Color color16 = GUI.color;
					float num27 = (color16.a = a12);
					Color val38 = (GUI.color = color16);
				}
				if (shoptype == 1 && shoparrayselected == num)
				{
					GUI.color = Color.yellow;
					float a13 = 0.2f;
					Color color17 = GUI.color;
					float num28 = (color17.a = a13);
					Color val40 = (GUI.color = color17);
				}
				GUI.DrawTexture(new Rect((float)(num7 - 250), (float)(Screen.height - 200 + 35 * num), 96f, 33f), SL_line, (ScaleMode)0, true, 0f);
				if (invent[num] >= 0)
				{
					GUI.color = Color.white;
					int num29 = 1;
					Color color18 = GUI.color;
					float num30 = (color18.a = num29);
					Color val42 = (GUI.color = color18);
					GUI.DrawTexture(new Rect((float)(num7 - 250), (float)(Screen.height - 200 + 35 * num), (float)itemlist[invent[num]].sprite.width, (float)itemlist[invent[num]].sprite.height), itemlist[invent[num]].sprite, (ScaleMode)0, true, 0f);
				}
			}
			for (; num < 10; num++)
			{
				GUI.color = Color.white;
				val2 = Input.mousePosition;
				val2.y = (float)Screen.height - Input.mousePosition.y;
				Rect val44 = new Rect((float)(num7 - 125), (float)(Screen.height - 200 + 35 * (num - 5)), 96f, 33f);
				if (val44.Contains(val2))
				{
					float a14 = 0.2f;
					Color color19 = GUI.color;
					float num31 = (color19.a = a14);
					Color val45 = (GUI.color = color19);
					if (Input.GetKeyDown((KeyCode)323))
					{
						shopselected = invent[num];
						shoparrayselected = num;
						update_gunshow();
						shoptype = 1;
					}
				}
				else
				{
					float a15 = 0.05f;
					Color color20 = GUI.color;
					float num32 = (color20.a = a15);
					Color val47 = (GUI.color = color20);
				}
				if (shoptype == 1 && shoparrayselected == num)
				{
					GUI.color = Color.yellow;
					float a16 = 0.2f;
					Color color21 = GUI.color;
					float num33 = (color21.a = a16);
					Color val49 = (GUI.color = color21);
				}
				GUI.DrawTexture(new Rect((float)(num7 - 125), (float)(Screen.height - 200 + 35 * (num - 5)), 96f, 33f), SL_line, (ScaleMode)0, true, 0f);
				if (invent[num] >= 0)
				{
					GUI.color = Color.white;
					int num34 = 1;
					Color color22 = GUI.color;
					float num35 = (color22.a = num34);
					Color val51 = (GUI.color = color22);
					GUI.DrawTexture(new Rect((float)(num7 - 125), (float)(Screen.height - 200 + 35 * (num - 5)), (float)itemlist[invent[num]].sprite.width, (float)itemlist[invent[num]].sprite.height), itemlist[invent[num]].sprite, (ScaleMode)0, true, 0f);
				}
			}
		}
		if (gamestate == 20)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 200), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[1].width / 2 - 30), (float)(Screen.height / 2 - 160), (float)(newhud[1].width + 60), (float)newhud[1].height), newhud[1], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 120), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 70), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 20), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 30), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 150), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			if (turnoff_LM)
			{
				if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 35), 400f, 32f), language[languageset].frase[112]))
				{
					turnoff_LM = false;
				}
			}
			else if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 35), 400f, 32f), language[languageset].frase[111]))
			{
				turnoff_LM = true;
			}
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 195), 400f, 32f), language[languageset].frase[83] + " " + ((Screen.resolutions[pretend_resolution])).width + "x" + ((Screen.resolutions[pretend_resolution])).height + " " + ((Screen.resolutions[pretend_resolution])).refreshRate + " Hz");
			pretend_resolution = (int)GUI.HorizontalSlider(new Rect((float)(num7 - 230), (float)(Screen.height / 2 - 150), 460f, 10f), (float)pretend_resolution, 0f, (float)(Screen.resolutions.Length - 1));
			string text = null;
			text = ((pretend_vsync != 0) ? ("x" + pretend_vsync.ToString()) : language[languageset].frase[85]);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 115), 400f, 32f), "V-Sync " + text))
			{
				pretend_vsync++;
				if (pretend_vsync > 2)
				{
					pretend_vsync = 0;
				}
			}
			text = ((pretend_antialias != 0) ? ("x" + pretend_antialias.ToString()) : language[languageset].frase[85]);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 65), 400f, 32f), "Anti-Alias " + text))
			{
				if (pretend_antialias == 0)
				{
					pretend_antialias = 2;
				}
				else
				{
					pretend_antialias *= 2;
				}
				if (pretend_antialias > 8)
				{
					pretend_antialias = 0;
				}
			}
			if (pretend_texture == 0)
			{
				num5 = 86;
			}
			if (pretend_texture == 1)
			{
				num5 = 87;
			}
			if (pretend_texture == 2)
			{
				num5 = 88;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 15), 400f, 32f), language[languageset].frase[82] + " = " + language[languageset].frase[num5]))
			{
				pretend_texture++;
				if (pretend_texture > 2)
				{
					pretend_texture = 0;
				}
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[84]))
			{
				QualitySettings.antiAliasing = pretend_antialias;
				QualitySettings.masterTextureLimit = pretend_texture;
				QualitySettings.vSyncCount = pretend_vsync;
				Screen.SetResolution(((Screen.resolutions[pretend_resolution])).width, ((Screen.resolutions[pretend_resolution])).height, Screen.fullScreen, ((Screen.resolutions[pretend_resolution])).refreshRate);
				mane_up_cont = 1;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 155), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 9;
			}
		}
		if (gamestate == 24)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 95), 400f, 32f), language[languageset].frase[99]))
			{
				iamserver = true;
				Network.InitializeSecurity();
				Network.InitializeServer(1, 50123, false);
				gamestate = 3;
				Application.LoadLevel(1);
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[97]))
			{
				gamestate = 25;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[98]))
			{
				MasterServer.ClearHostList();
				MasterServer.RequestHostList("Zumbiblocks " + buildversion);
				server_select = -1;
				gamestate = 26;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), language[languageset].frase[107]))
			{
				gamestate = 27;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
			}
		}
		if (gamestate == 28)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 85), 400f, 32f), language[languageset].frase[109]);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 170), (float)(Screen.height / 2 - 45), 100f, 32f), "Password");
			the_password = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 45), 200f, 30f), the_password, 10);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[108]))
			{
				gamestate = 5;
				iamserver = false;
				Application.LoadLevel(1);
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), language[languageset].frase[3]))
			{
				MasterServer.ClearHostList();
				MasterServer.RequestHostList("Zumbiblocks " + buildversion);
				server_select = -1;
				gamestate = 26;
			}
		}
		if (gamestate == 26)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.Box(new Rect(225f, 5f, (float)(Screen.width - 230), (float)(Screen.height - 250)), string.Empty);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height - 80), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height - 130), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			if (server_select < 0 || ZbMenu.Full(server_select))
			{
				GUI.color = Color.gray;
				GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height - 180), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
				GUI.Label(new Rect((float)(num7 - 100), (float)(Screen.height - 175), 200f, 32f), language[languageset].frase[108]);
			}
			else
			{
				GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height - 180), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
				GUI.color = Color.white;
				if (GUI.Button(new Rect((float)(num7 - 100), (float)(Screen.height - 175), 200f, 32f), language[languageset].frase[108]))
				{
					ipstoconec = hostData[server_select].ip;
					trynow = 0;
					ipstotry = hostData[server_select].ip.Length;
					porttoconec = hostData[server_select].port;
					porttostring = string.Empty + porttoconec;
					if (hostData[server_select].passwordProtected)
					{
						gamestate = 28;
					}
					else
					{
						gamestate = 5;
						iamserver = false;
						Application.LoadLevel(1);
					}
				}
			}
			GUI.color = Color.white;
			searching_server = GUI.TextField(new Rect((float)(num7 - 195), (float)(Screen.height - 225), 250f, 30f), searching_server, 10);
			GUI.DrawTexture(new Rect((float)(num7 + 70), (float)(Screen.height - 230), (float)searchsign.width, (float)searchsign.height), searchsign, (ScaleMode)0, true, 0f);
			GUI.color = Color.yellow;
			val2 = Input.mousePosition;
			val2.y = (float)Screen.height - Input.mousePosition.y;
			Rect val53 = new Rect((float)(num7 + 140), (float)(Screen.height - 230), (float)ref_sign.width, (float)ref_sign.height);
			if (val53.Contains(val2))
			{
				GUI.color = Color.green;
				if (Input.GetKeyDown((KeyCode)323))
				{
					MasterServer.ClearHostList();
					MasterServer.RequestHostList("Zumbiblocks " + buildversion);
					server_select = -1;
				}
			}
			GUI.DrawTexture(new Rect((float)(num7 + 140), (float)(Screen.height - 230), (float)ref_sign.width, (float)ref_sign.height), ref_sign, (ScaleMode)0, true, 0f);
			GUI.skin = newgui_square2;
			GUI.color = Color.red;
			GUI.Box(new Rect(225f, 5f, (float)(Screen.width - 230), 30f), string.Empty);
			GUI.Box(new Rect((float)(Screen.width - 25), 5f, 20f, (float)(Screen.height - 249)), string.Empty);
			GUI.Box(new Rect((float)(Screen.width - 25), 5f, 20f, 30f), string.Empty);
			GUI.skin = newgui_square2;
			GUI.color = Color.white;
			float a17 = 0.2f;
			Color color23 = GUI.color;
			float num36 = (color23.a = a17);
			Color val54 = (GUI.color = color23);
			GUI.DrawTexture(new Rect(270f, 10f, 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(Screen.width - 110), 10f, 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(Screen.width - 48), 10f, 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
			GUI.skin = newgui_square3;
			GUI.color = Color.white;
			GUI.Label(new Rect(275f, 10f, 90f, 19f), language[languageset].frase[100]);
			GUI.Label(new Rect(225f, 10f, 50f, 19f), "#");
			ZbMenu.ListHeader(this); // phase 3: Wave / Ping / Region / Players columns
			if (MasterServer.PollHostList().Length != 0)
			{
				hostData = MasterServer.PollHostList();
				num5 = hostData.Length;
				for (num = 0; 35 + 20 * num + 20 < Screen.height - 250; num++)
				{
				}
				listmaxroll = MasterServer.PollHostList().Length - num;
				if (listmaxroll < 0)
				{
					listmaxroll = 0;
				}
				GUI.skin = newgui_square;
				if (listmaxroll > 0)
				{
					listinicio = (int)GUI.VerticalSlider(new Rect((float)(Screen.width - 22), 37f, 20f, (float)(Screen.height - 249 - 40)), (float)listinicio, 0f, (float)listmaxroll);
				}
				num2 = listinicio;
				num = 0;
				for (; num2 < num5; num2++)
				{
					if (35 + 20 * num + 30 < Screen.height - 250 && strstr(hostData[num2].gameName, searching_server))
					{
						GUI.skin = newgui_square2;
						if (server_select == num2)
						{
							GUI.color = Color.yellow;
							float a18 = 0.2f;
							Color color24 = GUI.color;
							float num37 = (color24.a = a18);
							Color val56 = (GUI.color = color24);
							GUI.DrawTexture(new Rect(228f, (float)(37 + 20 * num), (float)(Screen.width - 255), 17f), SL_line, (ScaleMode)0, true, 0f);
						}
						GUI.color = Color.white;
						float a19 = 0.2f;
						Color color25 = GUI.color;
						float num38 = (color25.a = a19);
						Color val58 = (GUI.color = color25);
						GUI.DrawTexture(new Rect(228f, (float)(55 + 20 * num), (float)(Screen.width - 255), 1f), SL_line, (ScaleMode)0, true, 0f);
						GUI.DrawTexture(new Rect(270f, (float)(35 + 20 * num), 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
						GUI.DrawTexture(new Rect((float)(Screen.width - 110), (float)(35 + 20 * num), 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
						GUI.DrawTexture(new Rect((float)(Screen.width - 48), (float)(35 + 20 * num), 1f, 20f), SL_line, (ScaleMode)0, true, 0f);
						int num39 = 1;
						Color color26 = GUI.color;
						float num40 = (color26.a = num39);
						Color val60 = (GUI.color = color26);
						if (hostData[num2].passwordProtected)
						{
							GUI.DrawTexture(new Rect((float)(Screen.width - 43), (float)(37 + 20 * num), 13f, 16f), lockicon, (ScaleMode)0, true, 0f);
						}
						GUI.color = Color.white;
						val2 = Input.mousePosition;
						val2.y = (float)Screen.height - Input.mousePosition.y;
						GUI.skin = newgui_square3;
						GUI.Label(new Rect(230f, (float)(35 + 20 * num), 40f, 19f), num2.ToString());
						GUI.Label(new Rect((float)(Screen.width - 123), (float)(35 + 20 * num), 90f, 19f), hostData[num2].connectedPlayers.ToString() + "/" + hostData[num2].playerLimit);
						ZbMenu.ListRow(this, hostData[num2], (float)(35 + 20 * num));
						GUI.skin = newgui_square2;
						if (GUI.Button(new Rect(275f, (float)(35 + 20 * num), 400f, 19f), hostData[num2].gameName))
						{
							server_select = num2;
						}
						num++;
					}
				}
			}
			GUI.skin = newgui_square;
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 100), (float)(Screen.height - 125), 200f, 32f), language[languageset].frase[107]))
			{
				gamestate = 27;
			}
			if (GUI.Button(new Rect((float)(num7 - 100), (float)(Screen.height - 75), 200f, 32f), language[languageset].frase[3]))
			{
				gamestate = 24;
			}
		}
		if (gamestate == 27)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 170), (float)(Screen.height / 2 - 95), 100f, 32f), "Room code");
			GUI.Label(new Rect((float)(num7 - 170), (float)(Screen.height / 2 - 45), 100f, 32f), "Port");
			GUI.Label(new Rect((float)(num7 - 170), (float)(Screen.height / 2 + 5), 100f, 32f), "Password");
			ipstoconec[0] = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 95), 200f, 30f), ipstoconec[0], 15);
			porttostring = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 45), 200f, 30f), porttostring, 5);
			if (int.TryParse(porttostring, out porttoconec))
			{
				porttoconec = int.Parse(porttostring);
			}
			if (porttoconec == 0)
			{
				porttoconec = 25678;
			}
			the_password = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 + 5), 200f, 30f), the_password, 10);
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 100), (float)(Screen.height / 2 + 55), 200f, 32f), language[languageset].frase[108]))
			{
				gamestate = 5;
				iamserver = false;
				trynow = 0;
				ipstotry = 1;
				Application.LoadLevel(1);
			}
			if (GUI.Button(new Rect((float)(num7 - 100), (float)(Screen.height / 2 + 105), 200f, 32f), language[languageset].frase[3]))
			{
				gamestate = 24;
			}
		}
		if (gamestate == 25)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 200), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 150), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[2].width, (float)newhud[2].height), newhud[2], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 150), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - newhud[0].width / 2 + 5), (float)(Screen.height / 2 - 195), 200f, 32f), language[languageset].frase[100]);
			S_name = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 190), 200f, 30f), S_name, 10);
			GUI.Label(new Rect((float)(num7 - newhud[0].width / 2 + 5), (float)(Screen.height / 2 - 145), 200f, 32f), "Port");
			porttostring = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 140), 200f, 30f), porttostring, 5);
			if (int.TryParse(porttostring, out porttoconec))
			{
				porttoconec = int.Parse(porttostring);
			}
			if (porttoconec == 0)
			{
				porttoconec = 25678;
			}
			GUI.Label(new Rect((float)(num7 - newhud[0].width / 2 + 5), (float)(Screen.height / 2 - 95), 200f, 32f), "Password");
			Network.incomingPassword = GUI.TextField(new Rect((float)num7, (float)(Screen.height / 2 - 95), 200f, 30f), Network.incomingPassword, 10);
			if (saveinlist && GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[101]))
			{
				saveinlist = false;
			}
			if (!saveinlist && GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[102]))
			{
				saveinlist = true;
			}
			ZbMenu.LootToggle(this, num7); // playtest feature: Loot More / Classic
			ZbMenu.WavesToggle(this, num7); // playtest feature: Waves Timed / Classic (replaces the NAT toggle, which this port ignores)
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[105]))
			{
				if (S_name.Length < 4)
				{
					tooshort_s_name = true;
				}
				else
				{
					tooshort_s_name = false;
					iamserver = true;
					Network.InitializeSecurity();
					Network.InitializeServer(16, porttoconec, usarNat);
					if (saveinlist)
					{
						MasterServer.RegisterHost("Zumbiblocks " + buildversion, S_name, "Standart game");
					}
					gamestate = 3;
					Application.LoadLevel(1);
				}
			}
			if (tooshort_s_name)
			{
				GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 220), 400f, 32f), language[languageset].frase[106]);
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 155), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 24;
			}
		}
		if (gamestate == 0)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.color = Color.white;
			ZbMenu.MainMenuQuick(this, num7); // phase 3: Quick Play row above Play
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 95), 400f, 32f), language[languageset].frase[1]))
			{
				gamestate = 24;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[76]))
			{
				gamestate = 19;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[2]))
			{
				gamestate = 9;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[4]))
			{
				Application.Quit();
				Application.ExternalEval("window.close()");
			}
			GUI.color = Color.gray;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), language[languageset].frase[75]);
		}
		ZbMenu.Draw(this, num7); // phase 3: states 30 nickname, 31 Quick Play, 32 invite, 33 settings
		if (gamestate == 13)
		{
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 32f), language[languageset].frase[49]))
			{
				MasterServer.ClearHostList();
				MasterServer.RequestHostList("Zumbiblocks " + buildversion);
			}
			if (MasterServer.PollHostList().Length != 0)
			{
				hostData = MasterServer.PollHostList();
				num5 = hostData.Length;
				for (num = 0; Screen.height / 2 - 5 + 30 * num + 30 < Screen.height - 60; num++)
				{
				}
				listmaxroll = MasterServer.PollHostList().Length - num;
				if (listmaxroll < 0)
				{
					listmaxroll = 0;
				}
				GUI.skin = guiskin;
				if (listmaxroll > 0)
				{
					listinicio = (int)GUI.VerticalSlider(new Rect((float)(Screen.width / 2 - 15), (float)(Screen.height / 2 + 5), 15f, (float)(Screen.height - 70 - (Screen.height / 2 + 5))), (float)listinicio, 0f, (float)listmaxroll);
				}
				GUI.skin = guiSlist;
				num2 = listinicio;
				num = 0;
				for (; num2 < num5; num2++)
				{
					if (Screen.height / 2 - 5 + 30 * num + 30 < Screen.height - 60)
					{
						if (hostData[num2].passwordProtected)
						{
							GUI.DrawTexture(new Rect((float)(Screen.width / 2 - 35), (float)(Screen.height / 2 + 30 * num), 13f, 16f), lockicon, (ScaleMode)0, true, 0f);
						}
						GUI.color = Color.white;
						val2 = Input.mousePosition;
						val2.y = (float)Screen.height - Input.mousePosition.y;
						Rect val62 = new Rect((float)(Screen.width / 2 - 435), (float)(Screen.height / 2 - 5 + 30 * num), 400f, 25f);
						if (val62.Contains(val2))
						{
							GUI.Label(new Rect((float)(Screen.width / 2 + 10), (float)(Screen.height / 2 + 15), 150f, 25f), string.Empty + hostData[num2].connectedPlayers + "/" + (hostData[num2].playerLimit - 1) + language[languageset].frase[50]);
							GUI.Label(new Rect((float)(Screen.width / 2 + 10), (float)(Screen.height / 2 + 35), 200f, 25f), hostData[num2].ip[0] + (hostData[num2].port != 0 ? ":" + hostData[num2].port : ""));
							GUI.Label(new Rect((float)(Screen.width / 2 + 10), (float)(Screen.height / 2 + 55), 300f, 25f), hostData[num2].gameType);
							GUI.Label(new Rect((float)(Screen.width / 2 + 10), (float)(Screen.height / 2 + 75), 300f, 25f), hostData[num2].comment);
						}
						if (GUI.Button(new Rect((float)(Screen.width / 2 - 435), (float)(Screen.height / 2 - 5 + 30 * num), 400f, 25f), hostData[num2].gameName))
						{
							GUI.color = Color.white;
							ipstoconec = hostData[num2].ip;
							trynow = 0;
							ipstotry = hostData[num2].ip.Length;
							porttoconec = hostData[num2].port;
							porttostring = string.Empty + porttoconec;
							gamestate = 14;
							num2 = num5;
						}
					}
					num++;
				}
			}
			GUI.DrawTexture(new Rect((float)(Screen.width / 2), (float)(Screen.height / 2 + 5), 1f, (float)(Screen.height - 60 - Screen.height / 2 - 15)), SL_line, (ScaleMode)0, true, 0f);
			GUI.skin = guiskinerr;
			GUI.Label(new Rect((float)(Screen.width / 2 + 10), (float)(Screen.height / 2 - 10), 100f, 25f), "Info");
			GUI.skin = guiskin;
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height - 60), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 1;
			}
		}
		if (gamestate == 14)
		{
			GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 40f), language[languageset].frase[8]);
			playernick = GUI.TextField(new Rect((float)(Screen.width / 2 - 150), (float)(Screen.height / 2 - 0), 300f, 33f), playernick, 10);
			GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 43), 100f, 40f), "Pass");
			the_password = GUI.TextField(new Rect((float)(Screen.width / 2 - 100), (float)(Screen.height / 2 + 45), 300f, 35f), the_password, 19);
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 120), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 1;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 85), 400f, 32f), language[languageset].frase[10]))
			{
				gamestate = 5;
				iamserver = false;
				Application.LoadLevel(1);
			}
		}
		if (gamestate == 1)
		{
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 32f), language[languageset].frase[5]))
			{
				gamestate = 2;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 0), 400f, 32f), language[languageset].frase[48]) && !sp)
			{
				MasterServer.ClearHostList();
				MasterServer.RequestHostList("Zumbiblocks " + buildversion);
				gamestate = 13;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 35), 400f, 32f), language[languageset].frase[6]))
			{
				gamestate = 4;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 70), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
			}
		}
		if (gamestate == 15)
		{
			GUI.Label(new Rect((float)(Screen.width / 2 - 250), (float)(Screen.height / 2 - 30), 400f, 40f), "PORT");
			porttostring = GUI.TextField(new Rect((float)(Screen.width / 2 - 0), (float)(Screen.height / 2 - 30), 100f, 30f), porttostring, 5);
			if (int.TryParse(porttostring, out porttoconec))
			{
				porttoconec = int.Parse(porttostring);
			}
			if (porttoconec == 0)
			{
				porttoconec = 25678;
			}
			GUI.Label(new Rect((float)(Screen.width / 2 - 350), (float)(Screen.height / 2 + 45), 400f, 40f), "Pass");
			Network.incomingPassword = GUI.TextField(new Rect((float)(Screen.width / 2 - 100), (float)(Screen.height / 2 + 47), 300f, 35f), Network.incomingPassword, 19);
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 210), (float)(Screen.height / 2 + 5), 400f, 40f), "SinglePlayer"))
			{
				sp = !sp;
			}
			if (sp)
			{
				GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 100), (float)(Screen.height / 2 + 14), 32f, 22f), enabledsign, (ScaleMode)0, true, 0f);
			}
			if (!sp)
			{
				GUI.DrawTexture(new Rect((float)(Screen.width / 2 + 100), (float)(Screen.height / 2 + 15), 30f, 25f), disabledsign, (ScaleMode)0, true, 0f);
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 85), 400f, 40f), "Back"))
			{
				gamestate = 2;
			}
		}
		if (gamestate == 2)
		{
			testnat = !sp;
			saveinlist = !sp;
			GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 40), 400f, 40f), language[languageset].frase[8]);
			playernick = GUI.TextField(new Rect((float)(Screen.width / 2 - 150), (float)(Screen.height / 2 - 5), 300f, 38f), playernick, 10);
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 30), 400f, 40f), language[languageset].frase[54]))
			{
				gamestate = 15;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 70), 400f, 35f), language[languageset].frase[7]))
			{
				iamserver = true;
				bool flag = false;
				if (testnat)
				{
					if (!Network.HavePublicAddress())
					{
						Debug.Log((object)"nao tem ip publico");
						flag = true;
						Debug.Log((object)"usa o nat");
						ispub = false;
					}
					else
					{
						Debug.Log((object)"tem ip publico");
						NetworkPlayer player = Network.player;
						Network.connectionTesterIP = player.ipAddress;
						testvar = Network.TestConnection(false);
						if ((int)testvar != 2 || (int)testvar == -2)
						{
							Debug.Log((object)"publico nao conectavel");
							flag = true;
							Debug.Log((object)"usa o nat");
							ispub = false;
						}
						else
						{
							Debug.Log((object)"publico conectavel");
							ispub = true;
						}
					}
					cannat = flag;
				}
				Network.InitializeSecurity();
				Network.InitializeServer(16, porttoconec, flag);
				if (saveinlist)
				{
					Network.connectionTesterIP = MasterServer.ipAddress;
					testvar = Network.TestConnection(true);
					if ((int)testvar != -2)
					{
						MasterServer.RegisterHost("Zumbiblocks " + buildversion, playernick + "'s game", "Standart game");
						canlist = true;
						Debug.Log((object)"Testou a lista e deu certo");
					}
					else
					{
						Debug.Log((object)"Testou a lista e nao deu certo");
						canlist = false;
					}
				}
				gamestate = 3;
				Application.LoadLevel(1);
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 110), 400f, 40f), language[languageset].frase[3]))
			{
				gamestate = 1;
			}
		}
		if (gamestate == 3)
		{
		}
		if (gamestate == 4)
		{
			GUI.Label(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 - 35), 400f, 40f), language[languageset].frase[8]);
			playernick = GUI.TextField(new Rect((float)(Screen.width / 2 - 150), (float)(Screen.height / 2 - 0), 300f, 33f), playernick, 10);
			GUI.Label(new Rect((float)(Screen.width / 2 - 170), (float)(Screen.height / 2 + 35), 60f, 40f), "Ip:");
			ipstoconec[0] = GUI.TextField(new Rect((float)(Screen.width / 2 - 115), (float)(Screen.height / 2 + 38), 300f, 33f), ipstoconec[0], 40);
			GUI.Label(new Rect((float)(Screen.width / 2 - 450), (float)(Screen.height / 2 + 70), 400f, 40f), "PORT");
			porttostring = GUI.TextField(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 74), 100f, 30f), porttostring, 5);
			if (int.TryParse(porttostring, out porttoconec))
			{
				porttoconec = int.Parse(porttostring);
			}
			if (porttoconec == 0)
			{
				porttoconec = 25678;
			}
			GUI.Label(new Rect((float)(Screen.width / 2 - 100), (float)(Screen.height / 2 + 70), 100f, 40f), "Pass");
			the_password = GUI.TextField(new Rect((float)(Screen.width / 2 + 0), (float)(Screen.height / 2 + 74), 300f, 30f), the_password, 19);
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 150), (float)(Screen.height / 2 + 105), 140f, 32f), language[languageset].frase[3]))
			{
				gamestate = 1;
			}
			if (GUI.Button(new Rect((float)(Screen.width / 2 + 5), (float)(Screen.height / 2 + 105), 140f, 32f), language[languageset].frase[10]))
			{
				gamestate = 5;
				iamserver = false;
				trynow = 0;
				ipstotry = 1;
				Application.LoadLevel(1);
			}
		}
		if (gamestate == 5)
		{
			GUI.skin = guiskin;
			GUI.Label(new Rect((float)(Screen.width / 2 - 300), (float)(Screen.height / 2 - 20), 600f, 40f), language[languageset].frase[11]);
			if (GUI.Button(new Rect((float)(Screen.width / 2 - 200), (float)(Screen.height / 2 + 35), 400f, 40f), language[languageset].frase[12]))
			{
				Network.Disconnect(0);
				Application.LoadLevel(2);
				gamestate = 4;
			}
			GUI.skin = guiskinerr;
			GUI.Label(new Rect((float)(Screen.width / 2 - 400), (float)(Screen.height / 2 + 75), 800f, 20f), language[languageset].frase[13]);
			GUI.Label(new Rect((float)(Screen.width / 2 - 400), (float)(Screen.height / 2 + 100), 800f, 20f), language[languageset].frase[14]);
			GUI.skin = guiskin;
		}
		if (gamestate == 6)
		{
		}
		if (gamestate == 7)
		{
		}
		if (gamestate == 8)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 10), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.color = Color.yellow;
			GUI.Label(new Rect((float)(num7 - 300), (float)(Screen.height / 2 - 75), 600f, 40f), language[languageset].frase[15]);
			GUI.Label(new Rect((float)(num7 - 300), (float)(Screen.height / 2 - 50), 600f, 40f), language[languageset].frase[68] + " " + lastaccmoney + " $");
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 15), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
			}
		}
		if (gamestate == 9)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 55), 400f, 32f), "All settings")) // phase 3
			{
				gamestate = ZbMenu.ST_SETTINGS;
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 95), 400f, 32f), language[languageset].frase[16]))
			{
				gamestate = 10;
				errormsg = language[languageset].frase[9];
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[17]))
			{
				gamestate = 16;
				errormsg = language[languageset].frase[9];
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[18]))
			{
				gamestate = 20;
				pretend_antialias = QualitySettings.antiAliasing;
				pretend_texture = QualitySettings.masterTextureLimit;
				pretend_vsync = QualitySettings.vSyncCount;
				pretend_resolution = 0;
				for (num = 0; num < Screen.resolutions.Length; num++)
				{
					if (Screen.height == ((Screen.resolutions[num])).height && Screen.width == ((Screen.resolutions[num])).width)
					{
						pretend_resolution = num;
						num = Screen.resolutions.Length;
						Debug.Log((object)"encontrei minha resoluçao");
					}
				}
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
				errormsg = language[languageset].frase[9];
				save();
			}
		}
		if (gamestate == 16)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[1].width / 2 - 30), (float)(Screen.height / 2 - 40), (float)(newhud[1].width + 60), (float)newhud[1].height), newhud[1], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 80), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 300), (float)(Screen.height / 2 - 75), 600f, 32f), language[languageset].frase[55] + " = " + audiovol.ToString("F2"));
			audiovol = GUI.HorizontalSlider(new Rect((float)(num7 - 240), (float)(Screen.height / 2 - 33), 480f, 15f), audiovol, 0f, 1f);
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 9;
				errormsg = language[languageset].frase[9];
			}
		}
		if (gamestate == 10)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[1].width / 2 - 30), (float)(Screen.height / 2 - 40), (float)(newhud[1].width + 60), (float)newhud[1].height), newhud[1], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 130), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 80), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 - 0), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 50), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 100), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.Label(new Rect((float)(num7 - 300), (float)(Screen.height / 2 - 75), 600f, 32f), language[languageset].frase[20] + mousesense.ToString("F2"));
			mousesense = GUI.HorizontalSlider(new Rect((float)(num7 - 240), (float)(Screen.height / 2 - 32), 480f, 15f), mousesense, 0.2f, 3f);
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 125), 400f, 32f), language[languageset].frase[19]))
			{
				gamestate = 11;
			}
			if (GUI.Button(new Rect((float)(num7 - 230), (float)(Screen.height / 2 + 5), 400f, 32f), language[languageset].frase[30]))
			{
				autoreload = !autoreload;
			}
			if (autoreload)
			{
				GUI.DrawTexture(new Rect((float)(num7 + botaosizereload[languageset] - 50), (float)(Screen.height / 2 + 10), 32f, 22f), enabledsign, (ScaleMode)0, true, 0f);
			}
			if (!autoreload)
			{
				GUI.DrawTexture(new Rect((float)(num7 + botaosizereload[languageset] - 50), (float)(Screen.height / 2 + 10), 30f, 25f), disabledsign, (ScaleMode)0, true, 0f);
			}
			if (GUI.Button(new Rect((float)(num7 - 230), (float)(Screen.height / 2 + 55), 400f, 32f), language[languageset].frase[70]))
			{
				autoaim = !autoaim;
			}
			if (autoaim)
			{
				GUI.DrawTexture(new Rect((float)(num7 + botaosizeaim[languageset] - 50), (float)(Screen.height / 2 + 60), 32f, 22f), enabledsign, (ScaleMode)0, true, 0f);
			}
			if (!autoaim)
			{
				GUI.DrawTexture(new Rect((float)(num7 + botaosizeaim[languageset] - 50), (float)(Screen.height / 2 + 60), 30f, 25f), disabledsign, (ScaleMode)0, true, 0f);
			}
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 105), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 9;
				errormsg = language[languageset].frase[9];
				save();
			}
		}
		if (gamestate == 11)
		{
			sharedhud = 1;
			GUI.skin = guiskinerr;
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 100), 400f, 32f), language[languageset].frase[21]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 80), 400f, 32f), language[languageset].frase[22]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 65), 400f, 32f), language[languageset].frase[23]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 50), 400f, 32f), language[languageset].frase[24]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 35), 400f, 32f), language[languageset].frase[47]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 20), 400f, 32f), language[languageset].frase[25]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 5), 400f, 32f), language[languageset].frase[31]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 10), 400f, 32f), language[languageset].frase[46]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 25), 400f, 32f), language[languageset].frase[81]);
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 70), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 75), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 10;
				errormsg = language[languageset].frase[9];
			}
		}
		if (gamestate == 12)
		{
			sharedhud = 1;
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect((float)(num7 - newhud[0].width / 2), (float)(Screen.height / 2 + 10), (float)newhud[0].width, (float)newhud[0].height), newhud[0], (ScaleMode)0, true, 0f);
			GUI.skin = guiskinerr;
			GUI.color = Color.yellow;
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 105), 400f, 32f), language[languageset].frase[26]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 85), 400f, 32f), language[languageset].frase[27]);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 65), 400f, 32f), language[languageset].frase[28] + lastscore + language[languageset].frase[29]);
			string text2 = null;
			string text3 = null;
			text2 = string.Empty + timesurvivedmins;
			text3 = ((timesurvivedsecs >= 10) ? (string.Empty + timesurvivedsecs) : ("0" + timesurvivedsecs));
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 45), 400f, 32f), language[languageset].frase[57] + text2 + ":" + text3);
			GUI.Label(new Rect((float)(num7 - 200), (float)(Screen.height / 2 - 25), 400f, 32f), language[languageset].frase[68] + lastaccmoney + "$");
			GUI.skin = newgui_square;
			GUI.color = Color.white;
			if (GUI.Button(new Rect((float)(num7 - 200), (float)(Screen.height / 2 + 15), 400f, 32f), language[languageset].frase[3]))
			{
				gamestate = 0;
			}
		}
		if (sharedhud == 2)
		{
			if ((Object)(object)elmanequim != (Object)null && ((Component)elmanequim).gameObject.activeSelf)
			{
				((Component)elmanequim).gameObject.SetActive(false);
			}
			if ((Object)(object)item_show != (Object)null && !((Component)item_show).gameObject.activeSelf)
			{
				((Component)item_show).gameObject.SetActive(true);
			}
			GUI.color = Color.white;
			GUI.DrawTexture(new Rect(10f, 10f, (float)menulogo.width * 0.3f, (float)menulogo.height * 0.3f), menulogo, (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect((float)menulogo.width * 0.3f + 20f, 5f, 1f, (float)(Screen.height - 10)), SL_line, (ScaleMode)0, true, 0f);
			GUI.DrawTexture(new Rect(5f, 270f, 200f, 1f), SL_line, (ScaleMode)0, true, 0f);
			GUI.skin = newgui_square;
			GUI.color = Color.red;
			GUI.Box(new Rect(10f, (float)menulogo.height * 0.3f + 44f, 200f, 32f), string.Empty);
			GUI.Box(new Rect(10f, (float)menulogo.height * 0.3f + 350f, 200f, 28f), string.Empty);
			GUI.Box(new Rect(10f, (float)menulogo.height * 0.3f + 380f, 200f, 28f), string.Empty);
			GUI.skin.label.fontStyle = (FontStyle)0;
			GUI.color = Color.yellow;
			GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 381f, 175f, 25f), money + " $");
			if (shopselected >= 0)
			{
				GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 48f, 175f, 25f), itemlist[shopselected].nome);
				if (shoptype == 0)
				{
					GUI.color = Color.red;
					GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 351f, 175f, 25f), "-" + itemlist[shopselected].price + " $");
				}
				if (shoptype == 1)
				{
					GUI.color = Color.green;
					GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 351f, 175f, 25f), "+" + itemlist[shopselected].sellprice + " $");
				}
			}
			GUI.skin.label.fontStyle = (FontStyle)1;
			GUI.color = Color.white;
			GUI.skin = guiskinerr;
			GUI.Label(new Rect(25f, (float)menulogo.height * 0.3f + 12f, 150f, 25f), buildversion);
			GUI.skin = guiskin;
			if (shopselected >= 0)
			{
				GUI.color = Color.white;
				GUI.skin = newgui_square2;
				if (itemlist[shopselected].tipo == 0)
				{
					GUI.color = Color.yellow;
					GUI.Label(new Rect(5f, 275f, 400f, 20f), "DMG");
					GUI.Label(new Rect(5f, 295f, 400f, 20f), "RAT");
					if (!((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).isbazuka)
					{
						GUI.Label(new Rect(5f, 315f, 400f, 20f), "ACC");
					}
					else
					{
						GUI.Label(new Rect(5f, 315f, 400f, 20f), "RNG");
					}
					GUI.Label(new Rect(5f, 335f, 400f, 20f), "MAG");
					GUI.color = Color.green;
					if (!(((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano <= 250f))
					{
						num3 = ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano - 250f;
						GUI.color = Color.Lerp(Color.yellow, Color.red, num3 / 750f);
					}
					for (num = 0; !((float)(num * 10) >= ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano * (float)((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).bullets) && num * 6 < 150; num++)
					{
						float a20 = 0.5f;
						Color color27 = GUI.color;
						float num41 = (color27.a = a20);
						Color val63 = (GUI.color = color27);
						if (!(((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano * (float)((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).bullets - (float)(num * 10) <= 10f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
							continue;
						}
						num3 = ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano * (float)((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).bullets - (float)(num * 10);
						GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
					}
					GUI.color = Color.green;
					for (num = 0; !((float)(num * 5) >= ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).rate) && num * 6 < 150; num++)
					{
						float a21 = 0.5f;
						Color color28 = GUI.color;
						float num42 = (color28.a = a21);
						Color val65 = (GUI.color = color28);
						if (!(((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano - (float)(num * 5) <= 5f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
							continue;
						}
						num3 = ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).dano - (float)(num * 5);
						GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
					}
					if (!((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).isbazuka)
					{
						for (num = 0; !((float)(num * 10) >= (3f - ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).spread) * 50f) && num * 6 < 150; num++)
						{
							float a22 = 0.5f;
							Color color29 = GUI.color;
							float num43 = (color29.a = a22);
							Color val67 = (GUI.color = color29);
							if (!((3f - ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).spread) * 50f - (float)(num * 10) <= 10f))
							{
								GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
								continue;
							}
							num3 = (3f - ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).spread) * 50f - (float)(num * 10);
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
					}
					else
					{
						for (num = 0; !((float)(num * 3) >= ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).rangeofexp) && num * 6 < 150; num++)
						{
							float a23 = 0.5f;
							Color color30 = GUI.color;
							float num44 = (color30.a = a23);
							Color val69 = (GUI.color = color30);
							if (!(((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).rangeofexp - (float)(num * 3) <= 3f))
							{
								GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
								continue;
							}
							num3 = ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).rangeofexp - (float)(num * 3);
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
					}
					for (num = 0; num * 10 < ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).maxammo && num * 6 < 150; num++)
					{
						float a24 = 0.5f;
						Color color31 = GUI.color;
						float num45 = (color31.a = a24);
						Color val71 = (GUI.color = color31);
						if (!((float)(((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).maxammo - num * 10) <= 0.5f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 340f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
							continue;
						}
						num3 = ((sc_gunscript)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_gunscript))).maxammo - num * 10;
						GUI.DrawTexture(new Rect((float)(50 + num * 6), 340f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
					}
				}
				if (itemlist[shopselected].tipo == 2)
				{
					GUI.color = Color.yellow;
					GUI.Label(new Rect(5f, 275f, 400f, 20f), "DMG");
					GUI.Label(new Rect(5f, 295f, 400f, 20f), "RAT");
					GUI.Label(new Rect(5f, 315f, 400f, 20f), "RNG");
					if (((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).blunt)
					{
						GUI.Label(new Rect(5f, 335f, 400f, 20f), language[languageset].frase[73]);
					}
					else
					{
						GUI.Label(new Rect(5f, 335f, 400f, 20f), language[languageset].frase[74]);
					}
					GUI.color = Color.green;
					for (num = 0; num * 10 < ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).dmg && num * 6 < 150; num++)
					{
						float a25 = 0.5f;
						Color color32 = GUI.color;
						float num46 = (color32.a = a25);
						Color val73 = (GUI.color = color32);
						if (((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).dmg - num * 10 > 10)
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
							continue;
						}
						num3 = ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).dmg - num * 10;
						GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
					}
					for (num = 0; num * 10 < 100 - ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).endtime[0] && num * 6 < 150; num++)
					{
						float a26 = 0.5f;
						Color color33 = GUI.color;
						float num47 = (color33.a = a26);
						Color val75 = (GUI.color = color33);
						if (100 - ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).endtime[0] - num * 10 > 10)
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
							continue;
						}
						num3 = 100 - ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).endtime[0] - num * 10;
						GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
					}
					num = 0;
					num4 = 0f;
					while (!(num4 * 0.2f >= ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).range) && num * 6 < 150)
					{
						float a27 = 0.5f;
						Color color34 = GUI.color;
						float num48 = (color34.a = a27);
						Color val77 = (GUI.color = color34);
						if (!(((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).range - num4 * 0.2f <= 0.2f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						else
						{
							num3 = ((sc_meelegun)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_meelegun))).range - num4 * 0.2f;
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 320f, num3 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						num++;
						num4 = num;
					}
				}
				if (itemlist[shopselected].tipo == 4 && ((sc_throwable)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_throwable))).isgrenade && (Object)(object)(sc_granada)(object)((Component)((sc_throwable)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_throwable))).throw_prefab).GetComponent(typeof(sc_granada)) != (Object)null)
				{
					GUI.color = Color.yellow;
					GUI.Label(new Rect(5f, 275f, 400f, 20f), "DMG");
					GUI.Label(new Rect(5f, 295f, 400f, 20f), "RNG");
					GUI.color = Color.green;
					sc_granada sc_granada2 = (sc_granada)(object)((Component)((sc_throwable)(object)((Component)itemlist[shopselected].prefab).GetComponent(typeof(sc_throwable))).throw_prefab).GetComponent(typeof(sc_granada));
					num = 0;
					num4 = 0f;
					while (!(num4 * 35f >= sc_granada2.dano) && num * 6 < 150)
					{
						float a28 = 0.5f;
						Color color35 = GUI.color;
						float num49 = (color35.a = a28);
						Color val79 = (GUI.color = color35);
						if (!(sc_granada2.dano - num4 * 35f <= 35f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						else
						{
							num3 = sc_granada2.dano - num4 * 35f;
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 280f, num4 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						num++;
						num4 = num;
					}
					num = 0;
					num4 = 0f;
					while (!(num4 * 4f >= sc_granada2.exp_range) && num * 6 < 150)
					{
						float a29 = 0.5f;
						Color color36 = GUI.color;
						float num50 = (color36.a = a29);
						Color val81 = (GUI.color = color36);
						if (!(sc_granada2.dano - num4 * 4f <= 4f))
						{
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, 5f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						else
						{
							num3 = sc_granada2.dano - num4 * 4f;
							GUI.DrawTexture(new Rect((float)(50 + num * 6), 300f, num4 / 2f, 10f), SL_line, (ScaleMode)0, true, 0f);
						}
						num++;
						num4 = num;
					}
				}
			}
			GUI.color = Color.white;
			for (num = 0; num < language.Length; num++)
			{
				val2 = Input.mousePosition;
				val2.y = (float)Screen.height - Input.mousePosition.y;
				Rect val83 = new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f);
				if (val83.Contains(val2))
				{
					GUI.skin = guiskinerr;
					GUI.Label(new Rect(0f, (float)(Screen.height - 70), 140f, 25f), language[num].frase[0]);
					GUI.skin = guiskin;
				}
				GUI.DrawTexture(new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f), language[num].flag, (ScaleMode)0, true, 0f);
				if (GUI.Button(new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f), string.Empty))
				{
					languageset = num;
				}
			}
			float a30 = 0.5f;
			Color color37 = GUI.color;
			float num51 = (color37.a = a30);
			Color val84 = (GUI.color = color37);
			val2 = Input.mousePosition;
			val2.y = (float)Screen.height - Input.mousePosition.y;
			Rect val86 = new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height);
			if (val86.Contains(val2))
			{
				int num52 = 1;
				Color color38 = GUI.color;
				float num53 = (color38.a = num52);
				Color val87 = (GUI.color = color38);
				GUI.skin = guiSlist;
				GUI.Label(new Rect(130f, (float)(Screen.height - fscreenbuton.height - 35), 100f, 25f), "Fullscreen");
				GUI.skin = guiskin;
			}
			GUI.DrawTexture(new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height), fscreenbuton, (ScaleMode)0, true, 0f);
			int num54 = 1;
			Color color39 = GUI.color;
			float num55 = (color39.a = num54);
			Color val89 = (GUI.color = color39);
			if (GUI.Button(new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height), string.Empty))
			{
				if (!Screen.fullScreen)
				{
					screenlastx = Screen.width;
					screenlasty = Screen.height;
					Screen.SetResolution(((Screen.resolutions[Screen.resolutions.Length - 1])).width, ((Screen.resolutions[Screen.resolutions.Length - 1])).height, true);
				}
				else
				{
					Screen.SetResolution(screenlastx, screenlasty, false);
				}
				mane_up_cont = 1;
			}
		}
		if (sharedhud != 1)
		{
			return;
		}
		if ((Object)(object)elmanequim != (Object)null && (Screen.width != mane_w || Screen.height != mane_h))
		{
			update_manequim(); // WebGL port: page fullscreen / resize changed the screen size, place the mannequin again (the original's fullscreen button did this via mane_up_cont)
		}
		if ((Object)(object)elmanequim != (Object)null && !((Component)elmanequim).gameObject.activeSelf)
		{
			((Component)elmanequim).gameObject.SetActive(true);
		}
		if ((Object)(object)item_show != (Object)null && ((Component)item_show).gameObject.activeSelf)
		{
			((Component)item_show).gameObject.SetActive(false);
		}
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(10f, 10f, (float)menulogo.width * 0.3f, (float)menulogo.height * 0.3f), menulogo, (ScaleMode)0, true, 0f);
		GUI.DrawTexture(new Rect((float)menulogo.width * 0.3f + 20f, 5f, 1f, (float)(Screen.height - 10)), SL_line, (ScaleMode)0, true, 0f);
		GUI.skin = newgui_square;
		GUI.color = Color.red;
		GUI.Box(new Rect(10f, (float)menulogo.height * 0.3f + 45f, 200f, 28f), string.Empty);
		GUI.Box(new Rect(10f, (float)menulogo.height * 0.3f + 380f, 200f, 28f), string.Empty);
		GUI.skin.label.fontStyle = (FontStyle)0;
		GUI.color = Color.yellow;
		GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 46f, 175f, 25f), playernick);
		GUI.Label(new Rect(21f, (float)menulogo.height * 0.3f + 381f, 175f, 25f), money + " $");
		GUI.skin.label.fontStyle = (FontStyle)1;
		GUI.color = Color.white;
		GUI.skin = guiskinerr;
		GUI.Label(new Rect(25f, (float)menulogo.height * 0.3f + 12f, 150f, 25f), buildversion);
		GUI.Label(new Rect((float)(num7 - 400), (float)(Screen.height - 25), 800f, 20f), errormsg);
		GUI.skin = guiskin;
		for (num = 0; num < language.Length; num++)
		{
			val2 = Input.mousePosition;
			val2.y = (float)Screen.height - Input.mousePosition.y;
			Rect val91 = new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f);
			if (val91.Contains(val2))
			{
				GUI.skin = guiskinerr;
				GUI.Label(new Rect(0f, (float)(Screen.height - 70), 140f, 25f), language[num].frase[0]);
				GUI.skin = guiskin;
			}
			GUI.DrawTexture(new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f), language[num].flag, (ScaleMode)0, true, 0f);
			if (GUI.Button(new Rect((float)(10 + 40 * num), (float)(Screen.height - 40), 32f, 32f), string.Empty))
			{
				languageset = num;
			}
		}
		float a31 = 0.5f;
		Color color40 = GUI.color;
		float num56 = (color40.a = a31);
		Color val92 = (GUI.color = color40);
		val2 = Input.mousePosition;
		val2.y = (float)Screen.height - Input.mousePosition.y;
		Rect val94 = new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height);
		if (val94.Contains(val2))
		{
			int num57 = 1;
			Color color41 = GUI.color;
			float num58 = (color41.a = num57);
			Color val95 = (GUI.color = color41);
			GUI.skin = guiSlist;
			GUI.Label(new Rect(130f, (float)(Screen.height - fscreenbuton.height - 35), 100f, 25f), "Fullscreen");
			GUI.skin = guiskin;
		}
		GUI.DrawTexture(new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height), fscreenbuton, (ScaleMode)0, true, 0f);
		int num59 = 1;
		Color color42 = GUI.color;
		float num60 = (color42.a = num59);
		Color val97 = (GUI.color = color42);
		if (GUI.Button(new Rect(160f, (float)(Screen.height - fscreenbuton.height - 8), (float)fscreenbuton.width, (float)fscreenbuton.height), string.Empty))
		{
			if (!Screen.fullScreen)
			{
				screenlastx = Screen.width;
				screenlasty = Screen.height;
				Screen.SetResolution(((Screen.resolutions[Screen.resolutions.Length - 1])).width, ((Screen.resolutions[Screen.resolutions.Length - 1])).height, true);
			}
			else
			{
				Screen.SetResolution(screenlastx, screenlasty, false);
			}
			mane_up_cont = 1;
		}
	}

	public void cleanbodies(Transform notdelete)
	{
	}

	public void Main()
	{
	}
}

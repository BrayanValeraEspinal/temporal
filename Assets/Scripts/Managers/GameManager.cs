using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
	public int m_MaxLossesToEnd = 5;
	public float m_StartDelay = 3f;
	public float m_EndDelay = 3f;
	public float m_MaxGameTime = 240f;

	public CameraControl m_CameraControl;
	public Text m_MessageText;
	public Text m_NameText;
	public Text m_TimerText;
	public GameObject m_TankPrefab;
	public GameObject[] m_EnemyPrefabs;
	public Transform[] m_EnemySpawns;
	public AudioSource m_BackgroundMusic;
	public GameObject m_EndPanel;
	public TankManager[] m_Tanks;

	private int m_RoundNumber;
	private WaitForSeconds m_StartWait;
	private WaitForSeconds m_EndWait;
	private TankManager m_RoundWinner;
	private bool m_GameOver = false;
	private float m_TimeRemaining;

	private void Start()
	{
		m_StartWait = new WaitForSeconds(m_StartDelay);
		m_EndWait = new WaitForSeconds(m_EndDelay);
		m_TimeRemaining = m_MaxGameTime;

		if (m_BackgroundMusic != null)
		{
			m_BackgroundMusic.loop = true;
			m_BackgroundMusic.Play();
		}

		if (m_NameText != null)
			m_NameText.text = "Brayan Valera - 1-20-2488";

		if (m_EndPanel != null)
			m_EndPanel.SetActive(false);

		// Asignar cámara automáticamente si no está asignada en Inspector
		if (m_CameraControl == null)
		{
			m_CameraControl = FindObjectOfType<CameraControl>();
			if (m_CameraControl == null)
			{
				Debug.LogError("No se encontró CameraControl en la escena. Asigna manualmente en GameManager.");
				return;
			}
			else
			{
				Debug.Log("CameraControl asignado automáticamente en GameManager.");
			}
		}

		SpawnAllTanks();
		SetCameraTargets();
		SpawnEnemies();

		/*StartCoroutine(SpawnEnemiesPeriodically());*/
		StartCoroutine(GameLoop());
	}

	private void Update()
	{
		if (!m_GameOver)
		{
			m_TimeRemaining -= Time.deltaTime;
			if (m_TimerText != null)
				m_TimerText.text = FormatTime(m_TimeRemaining);

			if (m_TimeRemaining <= 0f)
			{
				m_TimeRemaining = 0f;
				m_GameOver = true;
				m_MessageText.text = GetFinalTimeOutMessage();

				if (m_EndPanel != null)
					m_EndPanel.SetActive(true);

				StartCoroutine(RestartAfterDelay());
			}
		}
	}

	public void RestartGame()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	private void SpawnAllTanks()
	{
		if (m_Tanks == null || m_Tanks.Length == 0)
		{
			Debug.LogError("No hay tanques asignados en GameManager (m_Tanks está vacío).");
			return;
		}

		for (int i = 0; i < m_Tanks.Length; i++)
		{
			m_Tanks[i].m_Instance = Instantiate(
				m_TankPrefab,
				m_Tanks[i].m_SpawnPoint.position,
				m_Tanks[i].m_SpawnPoint.rotation
			) as GameObject;

			m_Tanks[i].m_PlayerNumber = i + 1;
			m_Tanks[i].m_Losses = 0;
			m_Tanks[i].Setup();
		}
	}

	private void SetCameraTargets()
	{
		if (m_CameraControl == null)
		{
			Debug.LogError("m_CameraControl no está asignado en el Inspector.");
			return;
		}

		if (m_Tanks == null || m_Tanks.Length == 0)
		{
			Debug.LogError("m_Tanks no están asignados o están vacíos.");
			return;
		}

		Transform[] targets = new Transform[m_Tanks.Length];
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			if (m_Tanks[i] == null || m_Tanks[i].m_Instance == null)
			{
				Debug.LogError("Un elemento de m_Tanks está vacío o su instancia es null.");
				return;
			}

			targets[i] = m_Tanks[i].m_Instance.transform;
		}

		m_CameraControl.m_Targets = targets;
	}


	private void SpawnEnemies()
	{
		// Spawn solo 2 enemigos, en las primeras 2 posiciones de m_EnemySpawns si existen
		int enemiesToSpawn = Mathf.Min(2, m_EnemySpawns.Length);

		for (int i = 0; i < enemiesToSpawn; i++)
		{
			Instantiate(
				m_EnemyPrefabs[Random.Range(0, m_EnemyPrefabs.Length)],
				m_EnemySpawns[i].position,
				m_EnemySpawns[i].rotation
			);
		}
	}

	private IEnumerator SpawnEnemiesPeriodically()
	{
		while (!m_GameOver)
		{
			for (int i = 0; i < m_EnemySpawns.Length; i++)
			{
				Instantiate(
					m_EnemyPrefabs[Random.Range(0, m_EnemyPrefabs.Length)],
					m_EnemySpawns[i].position,
					m_EnemySpawns[i].rotation
				);
			}
			yield return new WaitForSeconds(15f);
		}
	}

	private IEnumerator GameLoop()
	{
		while (!m_GameOver)
		{
			yield return StartCoroutine(RoundStarting());
			yield return StartCoroutine(RoundPlaying());
			yield return StartCoroutine(RoundEnding());

			if (m_RoundNumber >= 5)
			{
				m_GameOver = true;
				m_MessageText.text = GetFinalResultsMessage();

				if (m_EndPanel != null)
					m_EndPanel.SetActive(true);

				Time.timeScale = 0f;
				yield break;
			}
		}
	}

	private IEnumerator RoundStarting()
	{
		ResetAllTanks();
		DisableTankControl();
		m_CameraControl.SetStartPositionAndSize();

		m_RoundNumber++;
		m_MessageText.text = "RONDA " + m_RoundNumber;

		yield return m_StartWait;
	}

	private IEnumerator RoundPlaying()
	{
		EnableTankControl();
		m_MessageText.text = "";

		while (!OneTankLeft())
		{
			if (m_GameOver) yield break;
			yield return null;
		}
	}

	private IEnumerator RoundEnding()
	{
		DisableTankControl();

		m_RoundWinner = GetRoundWinner();

		for (int i = 0; i < m_Tanks.Length; i++)
		{
			if (m_Tanks[i] != m_RoundWinner)
			{
				m_Tanks[i].m_Losses++;

				if (m_Tanks[i].m_Losses >= m_MaxLossesToEnd)
				{
					m_GameOver = true;
					m_MessageText.text = "JUGADOR " + m_Tanks[i].m_PlayerNumber + " PERDIÓ\n";

					for (int j = 0; j < m_Tanks.Length; j++)
					{
						if (j != i)
						{
							m_MessageText.text += "GANADOR: JUGADOR " + m_Tanks[j].m_PlayerNumber + "\n";
							m_MessageText.text += "PUNTAJE: " + (m_MaxLossesToEnd - m_Tanks[j].m_Losses) + "\n";
							m_MessageText.text += "TIEMPO: " + FormatTime(m_TimeRemaining);
							break;
						}
					}

					if (m_EndPanel != null)
						m_EndPanel.SetActive(true);

					yield return m_EndWait;
					Time.timeScale = 0f;
					yield break;
				}
			}
		}

		m_MessageText.text = GetRoundSummaryMessage();
		yield return m_EndWait;
	}

	private string GetRoundSummaryMessage()
	{
		string msg = "RONDA TERMINADA\n";
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			msg += "Jugador " + m_Tanks[i].m_PlayerNumber + ": " + m_Tanks[i].m_Losses + " derrotas\n";
		}
		return msg;
	}

	private string GetFinalResultsMessage()
	{
		string msg = "JUEGO COMPLETADO\n";
		int leastLosses = int.MaxValue;
		int winnerIndex = -1;
		bool empate = false;

		for (int i = 0; i < m_Tanks.Length; i++)
		{
			if (m_Tanks[i].m_Losses < leastLosses)
			{
				leastLosses = m_Tanks[i].m_Losses;
				winnerIndex = i;
				empate = false;
			}
			else if (m_Tanks[i].m_Losses == leastLosses)
			{
				empate = true;
			}
		}

		if (empate)
		{
			msg += "EMPATE ENTRE JUGADORES\n";
		}
		else
		{
			msg += "GANADOR: JUGADOR " + m_Tanks[winnerIndex].m_PlayerNumber + "\n";
			msg += "PUNTAJE: " + (m_MaxLossesToEnd - m_Tanks[winnerIndex].m_Losses) + "\n";
		}

		msg += "TIEMPO: " + FormatTime(m_TimeRemaining) + "\n\n";

		msg += "--- RESULTADOS ---\n";
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			msg += "Jugador " + m_Tanks[i].m_PlayerNumber + ": " + m_Tanks[i].m_Losses + " derrotas\n";
		}

		return msg;
	}

	private TankManager GetRoundWinner()
	{
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			if (m_Tanks[i].m_Instance.activeSelf)
				return m_Tanks[i];
		}
		return null;
	}

	private bool OneTankLeft()
	{
		int count = 0;
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			if (m_Tanks[i].m_Instance.activeSelf)
				count++;
		}
		return count <= 1;
	}

	private void ResetAllTanks()
	{
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			m_Tanks[i].Reset();
		}
	}

	private void EnableTankControl()
	{
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			m_Tanks[i].EnableControl();
		}
	}

	private void DisableTankControl()
	{
		for (int i = 0; i < m_Tanks.Length; i++)
		{
			m_Tanks[i].DisableControl();
		}
	}

	private IEnumerator RestartAfterDelay()
	{
		yield return new WaitForSeconds(4f);
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	private string FormatTime(float seconds)
	{
		if (seconds < 0f) seconds = 0f;
		int mins = Mathf.FloorToInt(seconds / 60f);
		int secs = Mathf.FloorToInt(seconds % 60f);
		return string.Format("{0:00}:{1:00}", mins, secs);
	}

	private string GetFinalTimeOutMessage()
	{
		string msg = "TIEMPO AGOTADO\n";
		return msg + GetFinalResultsMessage();
	}
}

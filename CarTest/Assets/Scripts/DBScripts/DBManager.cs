using System.Collections.Generic;
using DBScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Newtonsoft.Json;
using Npgsql;
using Photon.Pun;
using UnityEngine;

public class DBManager : MonoBehaviour
{
    public string playerName;
    public int playerId;

    private readonly string _connectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=Bobik123654;Database=postgres";

    public List<Detail> details;
    public DetailChangeColor detailChangeColor;

    private void Start()
    {
        detailChangeColor = FindObjectOfType<DetailChangeColor>();
    }

    /// <summary>
    /// Загрузка модификаций на машину игрока
    /// </summary>
    public void LoadModifications()
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM players", conn);
            NpgsqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var carModifications = JsonConvert.DeserializeObject<List<JsonFormat>>(dr.GetString(2)); // берется стобец json с модификациями машины
                if (dr.GetInt32(0) == playerId)
                    for (int j = 0; j < carModifications.Count; j++)
                    {
                        for (int i = 0; i < details.Count; i++)
                        {
                            if (details[i].detailValue == carModifications[j].DetailId)
                            {
                                details[i].gameObject.GetComponent<Renderer>().material.color =
                                    new Color(float.Parse(carModifications[j].DetailColor.Split()[0]),
                                    float.Parse(carModifications[j].DetailColor.Split()[1]),
                                    float.Parse(carModifications[j].DetailColor.Split()[2])); // изменение цветов деталей
                                details[i].gameObject.GetComponent<Renderer>().material.
                                    SetFloat("_Glossiness", carModifications[j].DetailSmothness); // изменение гладкости стекла
                            }
                        }
                    }
            }

            dr.Close();
        }
    }

    /// <summary>
    /// Сохраннеие модификаций игрока
    /// </summary>
    public void SaveModifications()
    {
        List<JsonFormat> carModifications = new List<JsonFormat>();
        string playerModifications = "";
        for (int i = 0; i < detailChangeColor.details.Count; i++)
        {
            Color color = detailChangeColor.details[i].gameObject.GetComponent<Renderer>().material.color; // сохранение цвета детали
            float smothness = detailChangeColor.details[i].gameObject.GetComponent<Renderer>().material.GetFloat("_Glossiness"); // сохраннение гладкости стекла
            if (!carModifications.Exists(x => x.DetailId == detailChangeColor.details[i].detailValue))
            {
                carModifications.Add(new JsonFormat(detailChangeColor.details[i].detailValue,
                    color.r + " " + color.g + " " + color.b,
                    smothness)); // добавление данных детали машины в список
                Hashtable hash = new Hashtable();
                hash.Add($"{detailChangeColor.details[i].detailValue}", $"{color.r} {color.g} {color.b} {smothness}");
                PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            }
        }

        var jsonString = JsonConvert.SerializeObject(carModifications); // сериализация в json
        playerModifications += jsonString;

        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString)) // запрос на вставку данных в таблицу players
        {
            conn.Open();

            NpgsqlCommand cmd;

            if (CheckPlayerIdExisting(playerId))
            {
                string updatePlayerModifications =
                    $"UPDATE players SET name = '{playerName}', player_modifications = '{playerModifications}' WHERE player_id = {playerId}";
                cmd = new NpgsqlCommand(updatePlayerModifications, conn);
            }
            else
            {
                string savePlayerModifications =
                    $"INSERT INTO players VALUES ({playerId}, '{playerName}', '{playerModifications}');";
                cmd = new NpgsqlCommand(savePlayerModifications, conn);
            }
            cmd.ExecuteNonQuery();
        }
    }

    public bool CheckPlayerIdExisting(int _playerId)
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            string checkPlayerId =
                $"SELECT EXISTS (SELECT * FROM players WHERE player_id = {_playerId})";
            NpgsqlCommand cmd =
                new NpgsqlCommand(checkPlayerId, conn);
            NpgsqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                return dr.GetBoolean(0);
            }
            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class Player
{
    public string id;
    public string name;
    public string position;   // GK, DEF, MID, FWD
    public int speed;         
    public int stamina;       
    public int defense;       
    public int attack;        
    public int goals;
    public int assists;

    public Player() { }

    public Player(string id, string name, string position,
                  int speed, int stamina, int defense, int attack,
                  int goals = 0, int assists = 0)
    {
        this.id      = id;
        this.name    = name;
        this.position = position;
        this.speed   = speed;
        this.stamina = stamina;
        this.defense = defense;
        this.attack  = attack;
        this.goals   = goals;
        this.assists = assists;
    }

    public int OverallRating => (speed + stamina + defense + attack) / 4;
}

[Serializable]
public class Team
{
    public string id;
    public string name;
    public string color;      
    public List<Player> players = new List<Player>();

    // Estadísticas de liga
    public int matchesPlayed;
    public int wins;
    public int draws;
    public int losses;
    public int goalsFor;
    public int goalsAgainst;

    public int Points       => wins * 3 + draws;
    public int GoalDiff     => goalsFor - goalsAgainst;

    public Team() { }

    public Team(string id, string name, string color)
    {
        this.id    = id;
        this.name  = name;
        this.color = color;
    }
}

[Serializable]
public class LeagueData
{
    public string leagueName = "Primera División";
    public List<Team> teams  = new List<Team>();
    public int maxSquadSize  = 3;   // controlado por Slider 2
    public int teamsShown    = 10;  // controlado por Slider 1

    // Lista de IDs de jugadores convocados (Pestaña 2)
    public List<string> calledUpPlayerIds = new List<string>();
}


[Serializable]
internal class LeagueDataWrapper { public LeagueData data; }

public static class DataManager
{
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "league_data.json");

    // Datos en memoria
    public static LeagueData CurrentData { get; private set; }

    // Guardar
    public static void Save()
    {
        if (CurrentData == null) return;
        string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[DataManager] Guardado en: {SavePath}");
    }

    //Cargar
    public static LeagueData Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                CurrentData = JsonUtility.FromJson<LeagueData>(json);
                Debug.Log("[DataManager] Datos cargados desde disco.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataManager] Error al cargar JSON: {e.Message}. Cargando datos por defecto.");
                CurrentData = GenerateDefaultData();
                Save();
            }
        }
        else
        {
            Debug.Log("[DataManager] No existe save file. Generando datos por defecto.");
            CurrentData = GenerateDefaultData();
            Save();
        }
        return CurrentData;
    }

    // Datos de ejemplo
    private static LeagueData GenerateDefaultData()
    {
        var data = new LeagueData { leagueName = "Liga Ejemplo", teamsShown = 10, maxSquadSize = 3 };

        var teamDefs = new (string id, string name, string color, int w, int d, int l, int gf, int ga)[]
        {
            ("t1",  "Málaga CF",   "#D4AF37", 8, 2, 0, 24, 7),
            ("t2",  "Real Celta de Vigo",  "#1A6BB5", 6, 3, 1, 18, 10),
            ("t3",  "Real Betis Balonpie",  "#E84A5F", 5, 3, 2, 15, 12),
            ("t4",  "Atletico de Madrid", "#2ECC71", 5, 2, 3, 14, 13),
            ("t5",  "FC Barcelona",      "#E67E22", 4, 3, 3, 12, 11),
            ("t6",  "Boca Juniors",     "#9B59B6", 3, 4, 3, 10, 11),
            ("t7",  "Real Madrid",      "#1ABC9C", 3, 2, 5, 9,  15),
            ("t8",  "Club Atletico River Plate",  "#E74C3C", 2, 3, 5, 8,  16),
            ("t9",  "Juventus",  "#34495E", 1, 4, 5, 7,  17),
            ("t10", "Valencia CF",   "#27AE60", 0, 2, 8, 4,  24),
        };

        int playerIdx = 1;
        foreach (var (id, name, color, w, d, l, gf, ga) in teamDefs)
        {
            var team = new Team(id, name, color)
            {
                matchesPlayed = w + d + l,
                wins   = w, draws = d, losses = l,
                goalsFor = gf, goalsAgainst = ga
            };

            string[] positions = { "GK", "DEF", "DEF", "MID", "MID", "FWD" };
            string[] pnames    =
            {
                "García", "López", "Martínez", "Rodríguez", "Sánchez",
                "González", "Fernández", "Pérez", "Gómez", "Díaz",
                "Torres", "Ruiz", "Jiménez", "Moreno", "Álvarez",
                "Romero", "Alonso", "Gutiérrez", "Navarro", "Ramos"
            };

            for (int i = 0; i < 6; i++)
            {
                int seed = playerIdx * 7 + i * 13;
                team.players.Add(new Player(
                    $"p{playerIdx}",
                    $"{pnames[(playerIdx - 1) % pnames.Length]} {i + 1}",
                    positions[i],
                    50 + (seed % 49), 50 + ((seed + 3) % 49),
                    50 + ((seed + 7) % 49), 50 + ((seed + 11) % 49),
                    (i == 5) ? (seed % 8) : 0,
                    (i == 4) ? (seed % 6) : 0
                ));
                playerIdx++;
            }
            data.teams.Add(team);
        }
        return data;
    }
}

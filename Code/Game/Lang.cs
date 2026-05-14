using System;
using System.Collections.Generic;
using Sandbox;

public enum AppLanguage
{
	English = 0,
	French = 1
}

/// <summary>
/// Système de traduction simple. Lookup par clé, fallback sur la clé brute si manquant.
/// La langue active est persistée via GameSettings.AppLanguage.
/// Les composants UI doivent inclure Lang.Current dans leur BuildHash pour re-render au switch.
/// </summary>
public static class Lang
{
	public static AppLanguage Current { get; private set; } = AppLanguage.English;

	public static event Action OnAppLanguageChanged;

	public static void SetAppLanguage( AppLanguage lang )
	{
		if ( Current == lang ) return;
		Current = lang;
		OnAppLanguageChanged?.Invoke();
	}

	public static string Get( string key )
	{
		if ( _table.TryGetValue( key, out var entries ) )
		{
			int idx = (int)Current;
			if ( idx >= 0 && idx < entries.Length ) return entries[idx];
		}
		return key;
	}

	public static string Get( string key, params object[] args )
	{
		var template = Get( key );
		try { return string.Format( template, args ); }
		catch { return template; }
	}

	// index 0 = English, 1 = French
	private static readonly Dictionary<string, string[]> _table = new()
	{
		// Main menu
		{ "menu.dev_banner",       new[] { "STILL IN DEVELOPMENT",         "EN DÉVELOPPEMENT" } },
		{ "menu.subtitle",         new[] { "A horror echolocation experience", "Une expérience d'horreur en écholocation" } },
		{ "menu.play",             new[] { "PLAY",                         "JOUER" } },
		{ "menu.settings",         new[] { "SETTINGS",                     "PARAMÈTRES" } },
		{ "menu.quit",             new[] { "QUIT",                         "QUITTER" } },
		{ "menu.join_discord",     new[] { "JOIN OUR DISCORD",             "REJOINDRE NOTRE DISCORD" } },
		{ "menu.link_copied",      new[] { "LINK COPIED — PASTE IN BROWSER", "LIEN COPIÉ — COLLEZ DANS LE NAVIGATEUR" } },
		{ "menu.footer",           new[] { "BlindHunter v1 — by Klibato",   "BlindHunter v1 — par Klibato" } },

		// Settings menu
		{ "settings.title",        new[] { "SETTINGS",                     "PARAMÈTRES" } },
		{ "settings.master_volume",new[] { "MASTER VOLUME",                "VOLUME GÉNÉRAL" } },
		{ "settings.sfx_volume",   new[] { "SFX VOLUME",                   "VOLUME EFFETS" } },
		{ "settings.music_volume", new[] { "MUSIC VOLUME",                 "VOLUME MUSIQUE" } },
		{ "settings.language",     new[] { "LANGUAGE",                     "LANGUE" } },
		{ "settings.english",      new[] { "ENGLISH",                      "ANGLAIS" } },
		{ "settings.french",       new[] { "FRENCH",                       "FRANÇAIS" } },
		{ "settings.preset.mute",  new[] { "MUTE",                         "MUET" } },
		{ "settings.preset.low",   new[] { "LOW",                          "BAS" } },
		{ "settings.preset.med",   new[] { "MED",                          "MOY" } },
		{ "settings.preset.full",  new[] { "FULL",                         "PLEIN" } },
		{ "settings.footer_note",  new[] { "Volume changes apply in V1.1 (sounds not yet routed through master)",
		                                   "Les changements de volume seront appliqués en V1.1 (sons pas encore routés sur le master)" } },
		{ "settings.back",         new[] { "BACK",                         "RETOUR" } },

		// Lobby room
		{ "lobby.title",           new[] { "LOBBY",                        "LOBBY" } },
		{ "lobby.starting_in",     new[] { "Starting in {0}",              "Démarrage dans {0}" } },
		{ "lobby.players",         new[] { "Players : {0}",                "Joueurs : {0}" } },
		{ "lobby.tag.you",         new[] { "YOU",                          "TOI" } },
		{ "lobby.tag.host",        new[] { "HOST",                         "HÔTE" } },
		{ "lobby.start_game",      new[] { "START GAME",                   "DÉMARRER" } },
		{ "lobby.waiting_host",    new[] { "Waiting for host to start the game...", "En attente du démarrage par l'hôte..." } },
		{ "lobby.need_more_players", new[] { "NEED AT LEAST {0} PLAYERS",   "{0} JOUEURS MIN REQUIS" } },

		// Pause menu
		{ "pause.title",           new[] { "MENU",                         "MENU" } },
		{ "pause.resume",          new[] { "RESUME",                       "REPRENDRE" } },
		{ "pause.settings",        new[] { "SETTINGS",                     "PARAMÈTRES" } },
		{ "pause.disconnect",      new[] { "DISCONNECT",                   "DÉCONNECTER" } },

		// Game over
		{ "gameover.they_escaped", new[] { "THEY ESCAPED",                 "ILS SE SONT ÉCHAPPÉS" } },
		{ "gameover.you_escaped",  new[] { "YOU ESCAPED",                  "TU T'ES ÉCHAPPÉ" } },
		{ "gameover.victory",      new[] { "VICTORY",                      "VICTOIRE" } },
		{ "gameover.you_died",     new[] { "YOU DIED",                     "TU ES MORT" } },
		{ "gameover.return_lobby", new[] { "RETURN TO LOBBY",              "RETOUR AU LOBBY" } },
		{ "gameover.waiting_host", new[] { "Waiting for host...",          "En attente de l'hôte..." } },

		// Scoreboard
		{ "scoreboard.title",      new[] { "PLAYERS",                      "JOUEURS" } },
		{ "scoreboard.quests",     new[] { "QUESTS",                       "QUÊTES" } },
		{ "scoreboard.col_name",   new[] { "PLAYER",                       "JOUEUR" } },
		{ "scoreboard.col_role",   new[] { "ROLE",                         "RÔLE" } },
		{ "scoreboard.col_status", new[] { "STATUS",                       "STATUT" } },
		{ "scoreboard.status.ready",new[]{ "Ready",                        "Prêt" } },
		{ "scoreboard.status.alive",new[]{ "Alive",                        "Vivant" } },
		{ "scoreboard.status.dead",new[] { "Dead",                         "Mort" } },
		{ "common.unknown",        new[] { "Unknown",                      "Inconnu" } },

		// Roles (also reused in scoreboard)
		{ "role.killer",           new[] { "Killer",                       "Tueur" } },
		{ "role.survivor",         new[] { "Survivor",                     "Survivant" } },

		// Role banner
		{ "role.banner.killer",    new[] { "YOU ARE THE KILLER",           "TU ES LE TUEUR" } },
		{ "role.banner.survivor",  new[] { "YOU ARE A SURVIVOR",           "TU ES UN SURVIVANT" } },

		// Quest HUD
		{ "quest.label",           new[] { "Quests : {0}/{1}",             "Quêtes : {0}/{1}" } },
		{ "quest.no_manager",      new[] { "(no manager)",                 "(aucun manager)" } },

		// Spectator
		{ "spectator.target",      new[] { "Spectating: {0}",              "Spectateur de : {0}" } },
		{ "spectator.hint",        new[] { "Press E to switch",            "Appuie sur E pour changer" } },

		// Interactables
		{ "prompt.interact",       new[] { "Press E to interact",          "Appuie sur E pour interagir" } },
		{ "prompt.activate_generator", new[] { "Press E to activate the generator", "Appuie sur E pour activer le générateur" } },
		{ "prompt.pickup",         new[] { "Pick up {0}",                  "Ramasser {0}" } },
		{ "prompt.keycard.with",   new[] { "Use Keycard",                  "Utiliser la carte" } },
		{ "prompt.keycard.without",new[] { "Need Keycard equipped",        "Carte requise (équipée)" } },
		{ "prompt.exit.unlocked",  new[] { "Escape through the door",      "S'échapper par la porte" } },
		{ "prompt.exit.locked",    new[] { "Door locked ({0}/{1})",        "Porte verrouillée ({0}/{1})" } },

		// Item names (for "Pick up {item}")
		{ "item.keycard",          new[] { "Keycard",                      "Carte" } },
		{ "item.stone",            new[] { "Stone",                        "Pierre" } },
		{ "item.none",             new[] { "None",                         "Aucun" } },

		// Quest group display names (looked up via QuestGroup.LocalizedName)
		{ "questgroup.generators", new[] { "Generators",                   "Générateurs" } },
	};
}

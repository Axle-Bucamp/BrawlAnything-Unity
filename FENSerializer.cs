using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Core;
using BrawlAnything.Character;

namespace BrawlAnything.Network
{
    /// <summary>
    /// Classe utilitaire pour la sérialisation et désérialisation au format FEN
    /// pour optimiser les communications réseau
    /// </summary>
    public class FENSerializer
    {
        // Séparateurs utilisés dans la notation FEN
        private const char SECTION_SEPARATOR = '/';
        private const char FIELD_SEPARATOR = ',';
        private const char SUBFIELD_SEPARATOR = ':';
        private const char ARRAY_START = '[';
        private const char ARRAY_END = ']';
        
        /// <summary>
        /// Sérialise les données de mise à jour de bataille au format FEN
        /// </summary>
        public string SerializeBattleUpdate(BattleUpdateData battleUpdate)
        {
            if (battleUpdate == null)
                return string.Empty;
                
            // Format FEN pour BattleUpdateData:
            // battleId/status/timeRemaining/[character1/character2/...]/[customData]
            
            List<string> sections = new List<string>();
            
            // Section 1: ID de bataille
            sections.Add(battleUpdate.battleId.ToString());
            
            // Section 2: Statut
            sections.Add(battleUpdate.status);
            
            // Section 3: Temps restant
            sections.Add(battleUpdate.timeRemaining.ToString("F1"));
            
            // Section 4: Personnages
            List<string> characterStrings = new List<string>();
            if (battleUpdate.characters != null)
            {
                foreach (CharacterStateData character in battleUpdate.characters)
                {
                    characterStrings.Add(SerializeCharacterState(character));
                }
            }
            sections.Add(ARRAY_START + string.Join(FIELD_SEPARATOR.ToString(), characterStrings) + ARRAY_END);
            
            // Section 5: Données personnalisées (optionnel)
            if (battleUpdate.customData != null && battleUpdate.customData.Count > 0)
            {
                List<string> customDataStrings = new List<string>();
                foreach (var kvp in battleUpdate.customData)
                {
                    customDataStrings.Add($"{kvp.Key}{SUBFIELD_SEPARATOR}{SerializeValue(kvp.Value)}");
                }
                sections.Add(ARRAY_START + string.Join(FIELD_SEPARATOR.ToString(), customDataStrings) + ARRAY_END);
            }
            else
            {
                sections.Add(ARRAY_START + ARRAY_END);
            }
            
            // Joindre toutes les sections
            return string.Join(SECTION_SEPARATOR.ToString(), sections);
        }
        
        /// <summary>
        /// Désérialise les données de mise à jour de bataille depuis le format FEN
        /// </summary>
        public BattleUpdateData DeserializeBattleUpdate(string fenData)
        {
            if (string.IsNullOrEmpty(fenData))
                return null;
                
            string[] sections = fenData.Split(SECTION_SEPARATOR);
            if (sections.Length < 4)
                return null;
                
            BattleUpdateData battleUpdate = new BattleUpdateData();
            
            // Section 1: ID de bataille
            if (int.TryParse(sections[0], out int battleId))
            {
                battleUpdate.battleId = battleId;
            }
            
            // Section 2: Statut
            battleUpdate.status = sections[1];
            
            // Section 3: Temps restant
            if (float.TryParse(sections[2], out float timeRemaining))
            {
                battleUpdate.timeRemaining = timeRemaining;
            }
            
            // Section 4: Personnages
            battleUpdate.characters = new List<CharacterStateData>();
            if (sections.Length > 3 && sections[3].Length > 2)
            {
                string charactersSection = sections[3].Substring(1, sections[3].Length - 2); // Enlever [ et ]
                string[] characterStrings = charactersSection.Split(FIELD_SEPARATOR);
                
                foreach (string characterString in characterStrings)
                {
                    CharacterStateData character = DeserializeCharacterState(characterString);
                    if (character != null)
                    {
                        battleUpdate.characters.Add(character);
                    }
                }
            }
            
            // Section 5: Données personnalisées (optionnel)
            battleUpdate.customData = new Dictionary<string, object>();
            if (sections.Length > 4 && sections[4].Length > 2)
            {
                string customDataSection = sections[4].Substring(1, sections[4].Length - 2); // Enlever [ et ]
                string[] customDataStrings = customDataSection.Split(FIELD_SEPARATOR);
                
                foreach (string dataString in customDataStrings)
                {
                    string[] keyValue = dataString.Split(SUBFIELD_SEPARATOR);
                    if (keyValue.Length == 2)
                    {
                        battleUpdate.customData[keyValue[0]] = DeserializeValue(keyValue[1]);
                    }
                }
            }
            
            return battleUpdate;
        }
        
        /// <summary>
        /// Sérialise les données d'état d'un personnage
        /// </summary>
        private string SerializeCharacterState(CharacterStateData character)
        {
            if (character == null)
                return string.Empty;
                
            // Format FEN pour CharacterStateData:
            // characterId:health:posX:posY:posZ:rotX:rotY:rotZ:animation:[effect1,effect2,...]
            
            List<string> fields = new List<string>();
            
            // Champ 1: ID du personnage
            fields.Add(character.characterId.ToString());
            
            // Champ 2: Santé actuelle
            fields.Add(character.currentHealth.ToString());
            
            // Champs 3-5: Position
            fields.Add(character.position.x.ToString("F2"));
            fields.Add(character.position.y.ToString("F2"));
            fields.Add(character.position.z.ToString("F2"));
            
            // Champs 6-8: Rotation
            fields.Add(character.rotation.eulerAngles.x.ToString("F1"));
            fields.Add(character.rotation.eulerAngles.y.ToString("F1"));
            fields.Add(character.rotation.eulerAngles.z.ToString("F1"));
            
            // Champ 9: Animation actuelle
            fields.Add(character.currentAnimation ?? "idle");
            
            // Champ 10: Effets de statut
            List<string> effectStrings = new List<string>();
            if (character.statusEffects != null)
            {
                foreach (StatusEffectData effect in character.statusEffects)
                {
                    effectStrings.Add(SerializeStatusEffect(effect));
                }
            }
            fields.Add(ARRAY_START + string.Join(FIELD_SEPARATOR.ToString(), effectStrings) + ARRAY_END);
            
            // Joindre tous les champs
            return string.Join(SUBFIELD_SEPARATOR.ToString(), fields);
        }
        
        /// <summary>
        /// Désérialise les données d'état d'un personnage
        /// </summary>
        private CharacterStateData DeserializeCharacterState(string characterString)
        {
            if (string.IsNullOrEmpty(characterString))
                return null;
                
            string[] fields = characterString.Split(SUBFIELD_SEPARATOR);
            if (fields.Length < 10)
                return null;
                
            CharacterStateData character = new CharacterStateData();
            
            // Champ 1: ID du personnage
            if (int.TryParse(fields[0], out int characterId))
            {
                character.characterId = characterId;
            }
            
            // Champ 2: Santé actuelle
            if (int.TryParse(fields[1], out int health))
            {
                character.currentHealth = health;
            }
            
            // Champs 3-5: Position
            float posX = 0, posY = 0, posZ = 0;
            float.TryParse(fields[2], out posX);
            float.TryParse(fields[3], out posY);
            float.TryParse(fields[4], out posZ);
            character.position = new Vector3(posX, posY, posZ);
            
            // Champs 6-8: Rotation
            float rotX = 0, rotY = 0, rotZ = 0;
            float.TryParse(fields[5], out rotX);
            float.TryParse(fields[6], out rotY);
            float.TryParse(fields[7], out rotZ);
            character.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            
            // Champ 9: Animation actuelle
            character.currentAnimation = fields[8];
            
            // Champ 10: Effets de statut
            character.statusEffects = new List<StatusEffectData>();
            if (fields.Length > 9 && fields[9].Length > 2)
            {
                string effectsSection = fields[9].Substring(1, fields[9].Length - 2); // Enlever [ et ]
                if (!string.IsNullOrEmpty(effectsSection))
                {
                    string[] effectStrings = effectsSection.Split(FIELD_SEPARATOR);
                    
                    foreach (string effectString in effectStrings)
                    {
                        StatusEffectData effect = DeserializeStatusEffect(effectString);
                        if (effect != null)
                        {
                            character.statusEffects.Add(effect);
                        }
                    }
                }
            }
            
            return character;
        }
        
        /// <summary>
        /// Sérialise les données d'un effet de statut
        /// </summary>
        private string SerializeStatusEffect(StatusEffectData effect)
        {
            if (effect == null)
                return string.Empty;
                
            // Format FEN pour StatusEffectData:
            // type:duration:intensity
            
            return $"{effect.type}{SUBFIELD_SEPARATOR}{effect.duration.ToString("F1")}{SUBFIELD_SEPARATOR}{effect.intensity.ToString("F1")}";
        }
        
        /// <summary>
        /// Désérialise les données d'un effet de statut
        /// </summary>
        private StatusEffectData DeserializeStatusEffect(string effectString)
        {
            if (string.IsNullOrEmpty(effectString))
                return null;
                
            string[] fields = effectString.Split(SUBFIELD_SEPARATOR);
            if (fields.Length < 3)
                return null;
                
            StatusEffectData effect = new StatusEffectData();
            
            // Champ 1: Type
            effect.type = fields[0];
            
            // Champ 2: Durée
            if (float.TryParse(fields[1], out float duration))
            {
                effect.duration = duration;
            }
            
            // Champ 3: Intensité
            if (float.TryParse(fields[2], out float intensity))
            {
                effect.intensity = intensity;
            }
            
            return effect;
        }
        
        /// <summary>
        /// Sérialise une valeur en chaîne de caractères
        /// </summary>
        private string SerializeValue(object value)
        {
            if (value == null)
                return "null";
                
            if (value is int intValue)
                return $"i{intValue}";
                
            if (value is float floatValue)
                return $"f{floatValue.ToString("F2")}";
                
            if (value is bool boolValue)
                return $"b{(boolValue ? "1" : "0")}";
                
            if (value is string stringValue)
                return $"s{stringValue}";
                
            return value.ToString();
        }
        
        /// <summary>
        /// Désérialise une valeur depuis une chaîne de caractères
        /// </summary>
        private object DeserializeValue(string valueString)
        {
            if (string.IsNullOrEmpty(valueString) || valueString == "null")
                return null;
                
            if (valueString.Length < 2)
                return valueString;
                
            char type = valueString[0];
            string data = valueString.Substring(1);
            
            switch (type)
            {
                case 'i': // Integer
                    if (int.TryParse(data, out int intValue))
                        return intValue;
                    break;
                    
                case 'f': // Float
                    if (float.TryParse(data, out float floatValue))
                        return floatValue;
                    break;
                    
                case 'b': // Boolean
                    return data == "1";
                    
                case 's': // String
                    return data;
            }
            
            return valueString;
        }
    }
}

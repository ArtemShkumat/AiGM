using System.Text;
using AiGMBackEnd.Models.Locations;

namespace AiGMBackEnd.Models.Prompts.Sections
{
    public class LocationContextSection : PromptSection
    {
        private readonly Location _location;

        public LocationContextSection(Location location)
        {
            _location = location;
        }

        public override void AppendTo(StringBuilder builder)
        {
            builder.AppendLine("# Current Location");
            builder.AppendLine($"Location Name: {_location.Name}");
            builder.AppendLine($"Location Type: {_location.Type}");
            builder.AppendLine($"Description: {_location.Description}");
            
            // Handle specific location types
            switch (_location)
            {
                case Delve delve:
                    AppendDelveDetails(builder, delve);
                    break;
                case Building building:
                    AppendBuildingDetails(builder, building);
                    break;
                case Settlement settlement:
                    AppendSettlementDetails(builder, settlement);
                    break;
            }
            
            builder.AppendLine();
        }

        private void AppendDelveDetails(StringBuilder builder, Delve delve)
        {
            builder.AppendLine($"Purpose: {delve.Purpose}");
            
            if (delve.Rooms != null && delve.Rooms.Count > 0)
            {
                builder.AppendLine("Rooms:");
                foreach (var room in delve.Rooms)
                {
                    builder.AppendLine($"- Room {room.RoomNumber}: {room.Name}");
                    builder.AppendLine($"  Role: {room.Role}");
                    builder.AppendLine($"  Description: {room.Description}");
                    
                    if (!string.IsNullOrEmpty(room.HazardOrGuardian))
                        builder.AppendLine($"  Hazard/Guardian: {room.HazardOrGuardian}");
                    
                    if (!string.IsNullOrEmpty(room.PuzzleOrRoleplayChallenge))
                        builder.AppendLine($"  Challenge: {room.PuzzleOrRoleplayChallenge}");
                    
                    if (!string.IsNullOrEmpty(room.TrickOrSetback))
                        builder.AppendLine($"  Trick/Setback: {room.TrickOrSetback}");
                    
                    if (!string.IsNullOrEmpty(room.ClimaxConflict))
                        builder.AppendLine($"  Climax: {room.ClimaxConflict}");
                    
                    if (!string.IsNullOrEmpty(room.RewardOrRevelation))
                        builder.AppendLine($"  Reward/Revelation: {room.RewardOrRevelation}");
                    
                    if (room.Valuables != null && room.Valuables.Count > 0)
                    {
                        builder.AppendLine("  Valuables:");
                        foreach (var valuable in room.Valuables)
                        {
                            builder.AppendLine($"    - {valuable.Name} (x{valuable.Quantity})");
                            builder.AppendLine($"      Value: {valuable.Value}");
                            builder.AppendLine($"      Location: {valuable.WhereExactly}");
                            builder.AppendLine($"      Description: {valuable.Description}");
                            builder.AppendLine($"      Why it's here: {valuable.WhyItsHere}");
                        }
                    }
                }
            }
        }

        private void AppendBuildingDetails(StringBuilder builder, Building building)
        {
            builder.AppendLine($"Purpose: {building.Purpose}");
            builder.AppendLine($"History: {building.History}");
            builder.AppendLine($"Exterior Description: {building.ExteriorDescription}");
            
            if (building.Floors != null && building.Floors.Count > 0)
            {
                builder.AppendLine("Floors:");
                foreach (var floor in building.Floors)
                {
                    builder.AppendLine($"- {floor.FloorName}");
                    builder.AppendLine($"  Description: {floor.Description}");
                    
                    if (floor.Rooms != null && floor.Rooms.Count > 0)
                    {
                        builder.AppendLine("  Rooms:");
                        foreach (var room in floor.Rooms)
                        {
                            builder.AppendLine($"    - {room.Name} ({room.Type})");
                            builder.AppendLine($"      Description: {room.Description}");
                            
                            if (room.PointsOfInterest != null && room.PointsOfInterest.Count > 0)
                            {
                                builder.AppendLine("      Points of Interest:");
                                foreach (var poi in room.PointsOfInterest)
                                {
                                    builder.AppendLine($"        - {poi.Name}: {poi.Description}");
                                }
                            }
                            
                            if (room.Valuables != null && room.Valuables.Count > 0)
                            {
                                builder.AppendLine("      Valuables:");
                                foreach (var valuable in room.Valuables)
                                {
                                    builder.AppendLine($"        - {valuable.Name}: {valuable.Description}");
                                }
                            }
                            
                            if (room.Npcs != null && room.Npcs.Count > 0)
                            {
                                builder.AppendLine($"      NPCs Present: {string.Join(", ", room.Npcs)}");
                            }
                            
                            if (room.Navigation != null && room.Navigation.ConnectedRooms.Count > 0)
                            {
                                builder.AppendLine($"      Connected Rooms: {string.Join(", ", room.Navigation.ConnectedRooms)}");
                            }
                        }
                    }
                }
            }
        }

        private void AppendSettlementDetails(StringBuilder builder, Settlement settlement)
        {
            builder.AppendLine($"Purpose: {settlement.Purpose}");
            builder.AppendLine($"History: {settlement.History}");
            builder.AppendLine($"Size: {settlement.Size}");
            builder.AppendLine($"Population: {settlement.Population}");
            
            if (settlement.Districts != null && settlement.Districts.Count > 0)
            {
                builder.AppendLine("Districts:");
                foreach (var district in settlement.Districts)
                {
                    builder.AppendLine($"- {district.Name}");
                    builder.AppendLine($"  Description: {district.Description}");
                    
                    if (district.ConnectedDistricts != null && district.ConnectedDistricts.Count > 0)
                    {
                        builder.AppendLine($"  Connected Districts: {string.Join(", ", district.ConnectedDistricts)}");
                    }
                    
                    if (district.PointsOfInterest != null && district.PointsOfInterest.Count > 0)
                    {
                        builder.AppendLine("  Points of Interest:");
                        foreach (var poi in district.PointsOfInterest)
                        {
                            builder.AppendLine($"    - {poi.Name}: {poi.Description}");
                        }
                    }
                    
                    if (district.Npcs != null && district.Npcs.Count > 0)
                    {
                        builder.AppendLine($"  NPCs Present: {string.Join(", ", district.Npcs)}");
                    }
                    
                    if (district.Buildings != null && district.Buildings.Count > 0)
                    {
                        builder.AppendLine($"  Buildings: {string.Join(", ", district.Buildings)}");
                    }
                }
            }
        }
    }
} 
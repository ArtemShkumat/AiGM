namespace AiGMBackEnd.Models.Locations
{
    /// <summary>
    /// A concrete implementation of the abstract Location class.
    /// Used for nested location types like Floor, Room, District, DelveRoom 
    /// that do not require unique properties beyond the base Location definition.
    /// </summary>
    public class GenericLocation : Location
    {
        // Parameterless constructor is required for deserialization and instantiation
        public GenericLocation() : base()
        {
        }
    }
} 
namespace MToGo.AgentBonusService.Models
{
    /// <summary>
    /// Response model matching AgentProfileResponse from Agent Service.
    /// </summary>
    public class AgentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

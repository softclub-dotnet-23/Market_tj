namespace MarketTJ.Application.Dto.ConversationDto;

public class CreateConversationDto
{
    // Null — чат ещё не привязан к заказу (вопрос фермеру до покупки).
    public int? OrderId { get; set; }
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public bool IsClosed { get; set; }
}

using System;

namespace Application.DTOs.CreateDialogDtos;

public class BaseCreateMessageDto
{
    public string Header { get; set; }
    public string Body { get; set; }
    public string Footer { get; set; }
    
    public virtual ListActionDto ListAction { get; set; }
    public virtual ButtonActionDto ButtonAction { get; set; }
}


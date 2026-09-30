using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public class AiSupportService : IAiSupportService
{
    private readonly IAiToolsService _aiToolsService;
    private readonly IAiSessionStore _sessionStore;
    private readonly AiSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiSupportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public AiSupportService(
        IAiToolsService aiToolsService,
        IAiSessionStore sessionStore,
        IOptions<AiSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<AiSupportService> logger)
    {
        _aiToolsService = aiToolsService;
        _sessionStore = sessionStore;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string GetSystemPrompt()
    {
        return 
@"You are the intelligent, polite, and highly accurate AI Customer Support Assistant for 'Raigon Arts' (a premium custom photo framing workshop located in Trivandrum, Kerala).

PRIMARY DIRECTIVES & CORE BEHAVIOR:
1. ACCURACY & INTEGRITY:
   - You must NEVER invent, assume, or hallucinate prices, availability, frame sizes, moulding materials, finishes, or order statuses.
   - All factual business data MUST be retrieved strictly by calling the appropriate AI business tool.
   - If a tool returns an error or no match, politely inform the customer based ONLY on the tool's output.

2. REGISTERED BUSINESS TOOLS:
   - 'get_products': To list framing categories and standard catalog frames.
   - 'get_sizes': To list available standard frame sizes and dimensions.
   - 'get_materials': To list supported moulding materials, frame types, colors, and glass options.
   - 'get_price': To calculate exact framing prices (including moulding, glass, mount, assembly, and GST).
   - 'check_availability': To check if a size/material is in stock and calculate workshop lead time.
   - 'get_order_status': To look up live customer order details using order number (e.g., 'RA-1069') or phone number.
   - 'create_order': To submit an order to the workshop database AFTER full customer confirmation.
   - 'transfer_to_human': To transfer the customer to a human workshop staff member when requested or in complex disputes.

3. CONVERSATION CONTEXT & ORDERING RULES:
   - Always remember customer details provided in the conversation (name, phone, frame size, materials, quantity, quoted price).
   - NEVER create an order immediately when details are incomplete.
   - Required before ordering: Customer Name, Phone Number, Frame Dimensions, Material/Color, and Quantity.
   - Before executing 'create_order', you MUST present a complete summary to the customer (Dimensions, Material, Quantity, Total Price, Delivery timeline) and receive their EXPLICIT confirmation (e.g. 'Yes', 'Confirm', 'Please place order').

4. HUMAN ESCALATION RULES:
   - If the customer asks to speak with a human, manager, or workshop staff member, call 'transfer_to_human' immediately.
   - If a customer has an unresolved issue, cancellation request, or refund dispute, call 'transfer_to_human'.
   - Once escalated, reassure the customer that workshop staff has been alerted and will reach out shortly.

5. TONE & STYLE:
   - Warm, welcoming, professional, and helpful.
   - Prices must be clearly stated in Indian Rupees (₹) with tax breakdowns if asked.
   - Provide clear, formatted WhatsApp-friendly responses using clean bullet points and emojis where appropriate.";
    }

    public IReadOnlyList<AiToolDefinition> GetToolDefinitions()
    {
        return new List<AiToolDefinition>
        {
            // 1. get_products
            new()
            {
                Function = new()
                {
                    Name = "get_products",
                    Description = "Retrieves framing product categories and active catalog items from the Raigon Arts database.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["category"] = new() { Type = "string", Description = "Optional filter by category (e.g., 'Standard Photo Frame', 'Custom Photo Frame', 'Certificate & Document Frame')." },
                            ["search"] = new() { Type = "string", Description = "Optional search query to filter frame products." }
                        },
                        Required = new()
                    }
                }
            },

            // 2. get_sizes
            new()
            {
                Function = new()
                {
                    Name = "get_sizes",
                    Description = "Retrieves all standard frame dimensions, aspect ratios, and starting prices available in the workshop.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["search"] = new() { Type = "string", Description = "Optional search string (e.g., '12x18', 'A4', 'Square', '16x24')." }
                        },
                        Required = new()
                    }
                }
            },

            // 3. get_materials
            new()
            {
                Function = new()
                {
                    Name = "get_materials",
                    Description = "Retrieves actual frame types, moulding materials, available colors/finishes, and supported glass options.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["frameType"] = new() { Type = "string", Description = "Optional filter for frame type (e.g., 'Wooden Frame', 'Synthetic / PS Moulding', 'Metal / Aluminium Frame', 'Acrylic Floating Frame')." }
                        },
                        Required = new()
                    }
                }
            },

            // 4. get_price
            new()
            {
                Function = new()
                {
                    Name = "get_price",
                    Description = "Calculates exact framing price quote deterministically based on dimensions, materials, mount, glass, and GST tax.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["width"] = new() { Type = "number", Description = "Frame width (in inches or specified unit)." },
                            ["height"] = new() { Type = "number", Description = "Frame height (in inches or specified unit)." },
                            ["unit"] = new() { Type = "string", Description = "Unit of measurement: 'inch' (default), 'cm', or 'mm'." },
                            ["frameType"] = new() { Type = "string", Description = "Frame type: 'Wooden Frame', 'Synthetic / PS Moulding', 'Metal / Aluminium Frame', etc." },
                            ["frameMaterial"] = new() { Type = "string", Description = "Specific material (e.g., 'Teak Wood Moulding', 'Synthetic PS Moulding', 'Aluminium Section')." },
                            ["glassType"] = new() { Type = "string", Description = "Glass option: 'Clear Float Glass', 'Non-Reflective / Anti-Glare Glass', 'Acrylic / Plexiglass (Shatterproof)', 'None (Canvas / Lamination)'." },
                            ["hasMount"] = new() { Type = "boolean", Description = "Whether a matboard / mount board border is included." },
                            ["quantity"] = new() { Type = "integer", Description = "Number of frames to order (default 1)." }
                        },
                        Required = new() { "width", "height" }
                    }
                }
            },

            // 5. check_availability
            new()
            {
                Function = new()
                {
                    Name = "check_availability",
                    Description = "Verifies active frame size availability in the database and computes current workshop turnaround/ready date.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["sizeOrDimensions"] = new() { Type = "string", Description = "Frame size name or dimensions (e.g., '12x18', 'FS-05', '8x10 inch', 'A4')." },
                            ["frameMaterial"] = new() { Type = "string", Description = "Optional moulding material to check." },
                            ["quantity"] = new() { Type = "integer", Description = "Quantity required (default 1)." }
                        },
                        Required = new() { "sizeOrDimensions" }
                    }
                }
            },

            // 6. create_order
            new()
            {
                Function = new()
                {
                    Name = "create_order",
                    Description = "Creates and saves a real customer custom framing order into the PostgreSQL database. NEVER call without explicit customer confirmation.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["customerName"] = new() { Type = "string", Description = "Customer full name." },
                            ["customerPhone"] = new() { Type = "string", Description = "Customer 10-digit or E.164 phone number." },
                            ["customerAltPhone"] = new() { Type = "string", Description = "Optional secondary phone number." },
                            ["customerCity"] = new() { Type = "string", Description = "Customer city." },
                            ["customerAddress"] = new() { Type = "string", Description = "Customer delivery address." },
                            ["customerPincode"] = new() { Type = "string", Description = "Customer postal code." },
                            ["frameSize"] = new() { Type = "string", Description = "Frame size (e.g., '12 × 18 inch', '8 × 10 inch')." },
                            ["unit"] = new() { Type = "string", Description = "Measurement unit ('inch', 'cm')." },
                            ["frameType"] = new() { Type = "string", Description = "Frame type ('Wooden Frame', 'Synthetic / PS Moulding')." },
                            ["frameMaterial"] = new() { Type = "string", Description = "Frame material ('Teak Wood Moulding', etc.)." },
                            ["frameColor"] = new() { Type = "string", Description = "Moulding color/finish ('Walnut Brown', 'Matte Black', 'Gold', etc.)." },
                            ["orientation"] = new() { Type = "string", Description = "'Landscape', 'Portrait', or 'Square'." },
                            ["quantity"] = new() { Type = "integer", Description = "Quantity of frames (minimum 1)." },
                            ["photoUrl"] = new() { Type = "string", Description = "Optional uploaded photo URL." },
                            ["photoName"] = new() { Type = "string", Description = "Optional photo label." },
                            ["advancePaid"] = new() { Type = "number", Description = "Advance payment amount if paid." },
                            ["deliveryDate"] = new() { Type = "string", Description = "Expected completion/delivery date (YYYY-MM-DD)." },
                            ["notes"] = new() { Type = "string", Description = "Special customer instructions or custom requests." }
                        },
                        Required = new() { "customerName", "customerPhone", "frameSize" }
                    }
                }
            },

            // 7. get_order_status
            new()
            {
                Function = new()
                {
                    Name = "get_order_status",
                    Description = "Retrieves live order progress, delivery timeline, and balance due from the database by Order Number (e.g. 'RA-1069') or Phone Number.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["orderNumberOrPhone"] = new() { Type = "string", Description = "Order number (e.g., 'RA-1069') or customer phone number." }
                        },
                        Required = new() { "orderNumberOrPhone" }
                    }
                }
            },

            // 8. transfer_to_human
            new()
            {
                Function = new()
                {
                    Name = "transfer_to_human",
                    Description = "Escalates conversation to workshop staff, creates an urgent dashboard notification, and pauses automated AI replies.",
                    Parameters = new()
                    {
                        Properties = new()
                        {
                            ["customerName"] = new() { Type = "string", Description = "Customer name." },
                            ["customerPhone"] = new() { Type = "string", Description = "Customer phone number." },
                            ["reason"] = new() { Type = "string", Description = "Reason for escalation (e.g., 'Customer requested human agent', 'Custom sizing query', 'Complaint')." },
                            ["channel"] = new() { Type = "string", Description = "'WhatsApp', 'Instagram', or 'Web'." },
                            ["chatSummary"] = new() { Type = "string", Description = "Brief context summary for workshop staff." }
                        },
                        Required = new() { "customerName", "customerPhone", "reason" }
                    }
                }
            }
        };
    }

    public async Task<AiToolCallResultDto> ExecuteToolCallAsync(
        string toolName,
        string argumentsJson,
        AiChatSession session,
        CancellationToken cancellationToken = default)
    {
        var callId = $"call_{Guid.NewGuid():N}";
        _logger.LogInformation("Executing AI Tool '{ToolName}' for session {SessionId}. Args: {Args}", toolName, session.SessionId, argumentsJson);

        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            switch (toolName.ToLowerInvariant())
            {
                case "get_products":
                {
                    var category = root.TryGetProperty("category", out var catProp) ? catProp.GetString() : null;
                    var search = root.TryGetProperty("search", out var sProp) ? sProp.GetString() : null;

                    var products = await _aiToolsService.GetProductsAsync(category, search);
                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(products, JsonOptions));
                }

                case "get_sizes":
                {
                    var search = root.TryGetProperty("search", out var sProp) ? sProp.GetString() : null;
                    var sizes = await _aiToolsService.GetSizesAsync(search);
                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(sizes, JsonOptions));
                }

                case "get_materials":
                {
                    var frameType = root.TryGetProperty("frameType", out var ftProp) ? ftProp.GetString() : null;
                    var materials = await _aiToolsService.GetMaterialsAsync(frameType);
                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(materials, JsonOptions));
                }

                case "get_price":
                {
                    var request = JsonSerializer.Deserialize<AiPriceCalculationRequest>(argumentsJson, JsonOptions) 
                                  ?? new AiPriceCalculationRequest();

                    var priceResult = await _aiToolsService.GetPriceAsync(request);

                    // Update session order draft context
                    _sessionStore.UpdateDraft(session.CustomerPhone, draft =>
                    {
                        draft.Width = request.Width;
                        draft.Height = request.Height;
                        draft.Unit = request.Unit;
                        draft.FrameSize = $"{request.Width:0.##} × {request.Height:0.##} {request.Unit}";
                        draft.FrameType = priceResult.FrameType;
                        draft.FrameMaterial = priceResult.FrameMaterial;
                        draft.GlassType = priceResult.GlassType;
                        draft.HasMount = priceResult.HasMount;
                        draft.Quantity = priceResult.Quantity;
                        draft.QuotedUnitPrice = priceResult.UnitPrice;
                        draft.QuotedTaxAmount = priceResult.TaxAmount;
                        draft.QuotedTotalPrice = priceResult.TotalPrice;
                        draft.IsPriceCalculated = true;
                    });

                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(priceResult, JsonOptions));
                }

                case "check_availability":
                {
                    var request = JsonSerializer.Deserialize<AiCheckAvailabilityRequest>(argumentsJson, JsonOptions)
                                  ?? new AiCheckAvailabilityRequest();

                    var availResult = await _aiToolsService.CheckAvailabilityAsync(request);

                    // Update session order draft context
                    _sessionStore.UpdateDraft(session.CustomerPhone, draft =>
                    {
                        if (!string.IsNullOrEmpty(availResult.MatchedSize))
                        {
                            draft.FrameSize = availResult.MatchedSize;
                        }
                        draft.EstimatedLeadTime = $"{availResult.EstimatedLeadDays} days (Ready: {availResult.EstimatedReadyDate})";
                    });

                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(availResult, JsonOptions));
                }

                case "create_order":
                {
                    var request = JsonSerializer.Deserialize<AiCreateOrderRequest>(argumentsJson, JsonOptions)
                                  ?? new AiCreateOrderRequest();

                    // Fallback to session draft if some fields omitted in tool args
                    if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                    {
                        request.CustomerPhone = session.CustomerPhone;
                    }
                    if (string.IsNullOrWhiteSpace(request.CustomerName) && !string.IsNullOrWhiteSpace(session.CustomerName))
                    {
                        request.CustomerName = session.CustomerName;
                    }

                    // Strict Safeguard: Verify minimum required fields
                    if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerPhone))
                    {
                        return AiToolCallResultDto.Error(toolName, callId,
                            "Order creation refused: Customer name and valid phone number are required before placing an order. Please ask the customer for their full name.");
                    }

                    // Strict Safeguard: Ensure details were confirmed by customer
                    if (!session.Draft.IsDetailsConfirmedByCustomer)
                    {
                        // Return informative note prompting the AI to request confirmation
                        return AiToolCallResultDto.Error(toolName, callId,
                            "Order confirmation required: Please summarize the complete order details (Customer Name, Size, Material, Quantity, and Total Price) and ask the customer for explicit confirmation before calling 'create_order'.");
                    }

                    var orderResult = await _aiToolsService.CreateOrderAsync(request);

                    if (orderResult.Success)
                    {
                        _sessionStore.UpdateDraft(session.CustomerPhone, draft =>
                        {
                            draft.IsOrderCreated = true;
                            draft.CreatedOrderNumber = orderResult.OrderNumber;
                        });
                    }

                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(orderResult, JsonOptions));
                }

                case "get_order_status":
                {
                    var query = root.TryGetProperty("orderNumberOrPhone", out var qProp) 
                        ? qProp.GetString() 
                        : (root.TryGetProperty("query", out var altProp) ? altProp.GetString() : session.CustomerPhone);

                    if (string.IsNullOrWhiteSpace(query))
                    {
                        query = session.CustomerPhone;
                    }

                    var statusResult = await _aiToolsService.GetOrderStatusAsync(query);
                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(statusResult, JsonOptions));
                }

                case "transfer_to_human":
                {
                    var request = JsonSerializer.Deserialize<AiTransferToHumanRequest>(argumentsJson, JsonOptions)
                                  ?? new AiTransferToHumanRequest();

                    if (string.IsNullOrWhiteSpace(request.CustomerPhone)) request.CustomerPhone = session.CustomerPhone;
                    if (string.IsNullOrWhiteSpace(request.CustomerName)) request.CustomerName = session.CustomerName ?? "Valued Customer";
                    if (string.IsNullOrWhiteSpace(request.Reason)) request.Reason = "Customer requested human assistance";

                    var transferResult = await _aiToolsService.TransferToHumanAsync(request);

                    // Halt AI replies on session
                    _sessionStore.SetHumanTakeover(session.CustomerPhone, true, request.Reason);

                    return AiToolCallResultDto.Ok(toolName, callId, JsonSerializer.Serialize(transferResult, JsonOptions));
                }

                default:
                    return AiToolCallResultDto.Error(toolName, callId, $"Unknown tool '{toolName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI tool '{ToolName}'", toolName);
            return AiToolCallResultDto.Error(toolName, callId, $"Tool execution failed: {ex.Message}");
        }
    }

    public async Task<AiChatResponseDto> ProcessCustomerMessageAsync(AiChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = _sessionStore.GetOrCreateSession(request.CustomerPhone, request.Channel, request.CustomerName);
        var toolsExecuted = new List<AiToolCallExecutionDto>();

        // 1. Check if session has been handed over to a human staff member
        if (session.IsHumanTakeover)
        {
            _logger.LogInformation("Session for {Phone} is currently under human takeover. Automatic AI response suppressed.", session.CustomerPhone);
            _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.User(request.Message, request.CustomerName));

            return new AiChatResponseDto
            {
                ReplyMessage = "Your conversation has been connected to our workshop staff. A team member will respond to you shortly! If you need immediate assistance, please call us at +91 70121 60065.",
                CustomerPhone = session.CustomerPhone,
                IsHumanTakeover = true,
                ToolsExecuted = toolsExecuted,
                OrderDraft = session.Draft,
                SessionId = session.SessionId,
                IsProviderConfigured = !string.IsNullOrWhiteSpace(_settings.ApiKey) || !string.IsNullOrWhiteSpace(_settings.GeminiApiKey),
                ProviderMessage = "Human takeover active. Automated bot replies paused."
            };
        }

        // 2. Add customer message to conversation history
        _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.User(request.Message, request.CustomerName));

        // 3. Update customer metadata if provided
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            _sessionStore.UpdateDraft(session.CustomerPhone, d => d.CustomerName = request.CustomerName);
        }
        if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            _sessionStore.UpdateDraft(session.CustomerPhone, d =>
            {
                d.PhotoUrl = request.PhotoUrl;
                d.PhotoName = request.PhotoName ?? "Customer Photo";
            });
        }

        // 4. Resolve Active AI Provider & Credentials
        var provider = string.IsNullOrWhiteSpace(_settings.Provider) ? "Groq" : _settings.Provider.Trim();
        var isGroq = provider.Equals("Groq", StringComparison.OrdinalIgnoreCase);
        var isGemini = provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase);
        var isOpenAi = provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase);

        var activeKey = isGroq
            ? _settings.GroqApiKey
            : (isGemini 
                ? (!string.IsNullOrWhiteSpace(_settings.GeminiApiKey) ? _settings.GeminiApiKey : _settings.ApiKey) 
                : _settings.ApiKey);

        var activeModel = isGroq
            ? (!string.IsNullOrWhiteSpace(_settings.GroqModel) ? _settings.GroqModel : "llama-3.3-70b-versatile")
            : (isGemini 
                ? (!string.IsNullOrWhiteSpace(_settings.GeminiModel) && !_settings.GeminiModel.StartsWith("gpt") ? _settings.GeminiModel : "gemini-1.5-flash") 
                : (!string.IsNullOrWhiteSpace(_settings.Model) ? _settings.Model : "gpt-4o-mini"));

        if (string.IsNullOrWhiteSpace(activeKey))
        {
            var keyConfigName = isGroq ? "AiAssistant:GroqApiKey" : (isGemini ? "AiAssistant:GeminiApiKey" : "AiAssistant:ApiKey");
            var keySettingProperty = isGroq ? "GroqApiKey" : (isGemini ? "GeminiApiKey" : "ApiKey");
            var warningMessage = $"⚠️ [AI Provider Not Configured]: No API Key found for provider '{provider}'.\n\n" +
                                 $"To enable dynamic natural-language AI replies with {provider}, configure your API key using one of the following:\n" +
                                 $"• Set via .NET User Secrets -> dotnet user-secrets set \"{keyConfigName}\" \"YOUR_KEY\"\n" +
                                 $"• Set via environment variable -> set {keyConfigName.Replace(":", "__")}=YOUR_KEY\n" +
                                 $"• Set in appsettings.json -> \"AiAssistant\": {{ \"{keySettingProperty}\": \"YOUR_KEY\" }}";

            _logger.LogWarning("AI Assistant API Key is missing for provider {Provider}. Please configure {KeyName}.", provider, keyConfigName);

            _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(warningMessage));

            return new AiChatResponseDto
            {
                ReplyMessage = warningMessage,
                CustomerPhone = session.CustomerPhone,
                IsHumanTakeover = false,
                ToolsExecuted = toolsExecuted,
                OrderDraft = session.Draft,
                SessionId = session.SessionId,
                IsProviderConfigured = false,
                ProviderMessage = $"AI Provider '{provider}' unconfigured. Please configure {keyConfigName}."
            };
        }

        // 5. Execute Multi-Turn Function Calling Loop with Active Provider
        if (isGroq)
        {
            return await ExecuteGroqConversationLoopAsync(request, session, activeKey, activeModel, cancellationToken);
        }
        else if (isGemini)
        {
            return await ExecuteGeminiConversationLoopAsync(request, session, activeKey, activeModel, cancellationToken);
        }
        else
        {
            return await ExecuteOpenAiConversationLoopAsync(request, session, activeKey, activeModel, cancellationToken);
        }
    }

    /// <summary>
    /// Google Gemini multi-turn function calling conversation loop using Gemini REST API.
    /// Endpoint: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent
    /// Authenticates securely via 'x-goog-api-key' request header.
    /// </summary>
    private async Task<AiChatResponseDto> ExecuteGeminiConversationLoopAsync(
        AiChatRequestDto request,
        AiChatSession session,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var toolsExecuted = new List<AiToolCallExecutionDto>();
        var client = _httpClientFactory.CreateClient("AiAssistantClient");

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        var toolDefs = GetToolDefinitions();
        var systemPrompt = GetSystemPrompt();

        // Build Gemini Tools Declaration
        var geminiTools = new[]
        {
            new
            {
                functionDeclarations = toolDefs.Select(t => new
                {
                    name = t.Function.Name,
                    description = t.Function.Description,
                    parameters = new
                    {
                        type = "OBJECT",
                        properties = t.Function.Parameters.Properties.ToDictionary(
                            p => p.Key,
                            p => new
                            {
                                type = MapToJsonSchemaType(p.Value.Type),
                                description = p.Value.Description
                            }
                        ),
                        required = t.Function.Parameters.Required ?? new List<string>()
                    }
                }).ToList()
            }
        };

        // Convert session history to Gemini contents format
        var geminiContents = new List<object>();

        foreach (var msg in session.Messages)
        {
            if (msg.Role == "user")
            {
                geminiContents.Add(new
                {
                    role = "user",
                    parts = new object[] { new { text = msg.Content ?? "" } }
                });
            }
            else if (msg.Role == "assistant")
            {
                if (!string.IsNullOrWhiteSpace(msg.RawModelPartsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(msg.RawModelPartsJson);
                        geminiContents.Add(new
                        {
                            role = "model",
                            parts = doc.RootElement.Clone()
                        });
                    }
                    catch
                    {
                        geminiContents.Add(new
                        {
                            role = "model",
                            parts = new object[] { new { text = msg.Content ?? "" } }
                        });
                    }
                }
                else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    var parts = msg.ToolCalls.Select(tc => new
                    {
                        functionCall = new
                        {
                            name = tc.Function.Name,
                            args = ParseJsonOrObject(tc.Function.Arguments)
                        }
                    }).ToArray();

                    geminiContents.Add(new
                    {
                        role = "model",
                        parts
                    });
                }
                else
                {
                    geminiContents.Add(new
                    {
                        role = "model",
                        parts = new object[] { new { text = msg.Content ?? "" } }
                    });
                }
            }
            else if (msg.Role == "tool")
            {
                geminiContents.Add(new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            functionResponse = new
                            {
                                name = msg.Name ?? "tool_result",
                                response = new
                                {
                                    name = msg.Name ?? "tool_result",
                                    content = ParseJsonOrObject(msg.Content)
                                }
                            }
                        }
                    }
                });
            }
        }

        // Multi-turn tool execution loop (up to 5 turns)
        var maxIterations = 5;
        string? finalAssistantReply = null;
        string? providerErrorMessage = null;

        try
        {
            for (int turn = 0; turn < maxIterations; turn++)
            {
                var reqBody = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = geminiContents,
                    tools = geminiTools,
                    generationConfig = new
                    {
                        temperature = _settings.Temperature,
                        maxOutputTokens = _settings.MaxTokens > 0 ? _settings.MaxTokens : 1500
                    }
                };

                HttpResponseMessage? httpRes = null;
                string resJson = string.Empty;
                var maxHttpAttempts = 3;

                for (int attempt = 1; attempt <= maxHttpAttempts; attempt++)
                {
                    var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(reqBody, JsonOptions), Encoding.UTF8, "application/json")
                    };
                    httpReq.Headers.Add("x-goog-api-key", apiKey);

                    try
                    {
                        httpRes = await client.SendAsync(httpReq, cancellationToken);
                        resJson = await httpRes.Content.ReadAsStringAsync(cancellationToken);

                        if (httpRes.IsSuccessStatusCode)
                        {
                            break;
                        }

                        var statusCode = (int)httpRes.StatusCode;
                        var isTransient = statusCode == 503 || statusCode == 502 || statusCode == 504 || statusCode == 429;

                        if (isTransient && attempt < maxHttpAttempts)
                        {
                            var delayMs = attempt * 1000;
                            _logger.LogWarning("Gemini request returned transient HTTP {Status} on attempt {Attempt}/{Max}. Retrying in {Delay}ms...",
                                statusCode, attempt, maxHttpAttempts, delayMs);
                            await Task.Delay(delayMs, cancellationToken);
                            continue;
                        }

                        // Non-transient error or exhausted retries
                        break;
                    }
                    catch (HttpRequestException ex) when (attempt < maxHttpAttempts)
                    {
                        var delayMs = attempt * 1000;
                        _logger.LogWarning(ex, "Gemini network error on attempt {Attempt}/{Max}. Retrying in {Delay}ms...", attempt, maxHttpAttempts, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }

                if (httpRes == null || !httpRes.IsSuccessStatusCode)
                {
                    var statusCode = httpRes != null ? (int)httpRes.StatusCode : 0;
                    string errorSummary = $"HTTP {statusCode} ({(httpRes != null ? httpRes.StatusCode.ToString() : "Network Error")})";
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(resJson))
                        {
                            using var errDoc = JsonDocument.Parse(resJson);
                            if (errDoc.RootElement.TryGetProperty("error", out var errElem))
                            {
                                var msg = errElem.TryGetProperty("message", out var mProp) ? mProp.GetString() : null;
                                var status = errElem.TryGetProperty("status", out var sProp) ? sProp.GetString() : null;
                                errorSummary = $"{status ?? "ERROR"}: {msg ?? "API Error"}";
                            }
                        }
                    }
                    catch
                    {
                        // Keep default status
                    }

                    _logger.LogError("Gemini completion request failed with status {Status}: {ErrorSummary}", statusCode, errorSummary);
                    providerErrorMessage = errorSummary;

                    if (statusCode == 503 || statusCode == 502 || statusCode == 504 || statusCode == 429)
                    {
                        finalAssistantReply = "The AI service is temporarily busy. Please try again shortly.";
                    }
                    else if (statusCode == 401 || statusCode == 403)
                    {
                        finalAssistantReply = $"⚠️ [Google Gemini Authentication Error (HTTP {statusCode})]: {errorSummary}\n\n" +
                                             $"Please verify that your Gemini API key in .NET User Secrets ('AiAssistant:GeminiApiKey') is valid.";
                    }
                    else
                    {
                        finalAssistantReply = $"⚠️ [Google Gemini API Error (HTTP {statusCode})]: {errorSummary}";
                    }

                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }

                using var doc = JsonDocument.Parse(resJson);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidatesElem) || candidatesElem.GetArrayLength() == 0)
                {
                    finalAssistantReply = "⚠️ [Google Gemini Error]: No response candidates were returned by the Gemini model.";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }

                var candidate = candidatesElem[0];
                if (!candidate.TryGetProperty("content", out var contentElem) || !contentElem.TryGetProperty("parts", out var partsElem))
                {
                    finalAssistantReply = "⚠️ [Google Gemini Error]: The model returned an empty content payload.";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }

                // Check for functionCall parts
                var functionCalls = new List<(string Name, string ArgsJson, string? Id)>();
                var textParts = new List<string>();

                foreach (var part in partsElem.EnumerateArray())
                {
                    if (part.TryGetProperty("functionCall", out var fcElem))
                    {
                        var fnName = fcElem.TryGetProperty("name", out var fnProp) ? fnProp.GetString() ?? "" : "";
                        var fnArgs = fcElem.TryGetProperty("args", out var argsProp) ? argsProp.GetRawText() : "{}";
                        var fnId = fcElem.TryGetProperty("id", out var idProp) 
                            ? idProp.GetString() 
                            : (part.TryGetProperty("id", out var partIdProp) ? partIdProp.GetString() : null);
                        functionCalls.Add((fnName, fnArgs, fnId));
                    }
                    else if (part.TryGetProperty("text", out var textProp))
                    {
                        var t = textProp.GetString();
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            textParts.Add(t);
                        }
                    }
                }

                if (functionCalls.Count > 0)
                {
                    // PRESERVE the exact model parts from Gemini (including thoughtSignature, functionCall, id, etc.)
                    var rawModelPartsJson = partsElem.GetRawText();

                    geminiContents.Add(new
                    {
                        role = "model",
                        parts = partsElem.Clone()
                    });

                    var toolCallsInfoList = functionCalls.Select(fc => new AiToolCallInfo
                    {
                        Id = !string.IsNullOrWhiteSpace(fc.Id) ? fc.Id : $"call_{Guid.NewGuid():N}",
                        Function = new AiFunctionCallDetail { Name = fc.Name, Arguments = fc.ArgsJson }
                    }).ToList();

                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(null, toolCallsInfoList, rawModelPartsJson));

                    // Execute each tool call against live PostgreSQL database via AiToolsService
                    var toolResponseParts = new List<object>();

                    for (int i = 0; i < functionCalls.Count; i++)
                    {
                        var fc = functionCalls[i];
                        var toolRes = await ExecuteToolCallAsync(fc.Name, fc.ArgsJson, session, cancellationToken);

                        toolsExecuted.Add(new AiToolCallExecutionDto
                        {
                            ToolName = fc.Name,
                            ToolCallId = toolCallsInfoList[i].Id,
                            Success = toolRes.Success,
                            ArgumentsJson = fc.ArgsJson,
                            ResultJson = toolRes.ResultJson,
                            ErrorMessage = toolRes.ErrorMessage
                        });

                        // Record tool result in session
                        _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Tool(toolRes.ResultJson, toolCallsInfoList[i].Id, fc.Name));

                        if (!string.IsNullOrWhiteSpace(fc.Id))
                        {
                            toolResponseParts.Add(new
                            {
                                functionResponse = new
                                {
                                    name = fc.Name,
                                    response = new
                                    {
                                        name = fc.Name,
                                        content = ParseJsonOrObject(toolRes.ResultJson)
                                    },
                                    id = fc.Id
                                }
                            });
                        }
                        else
                        {
                            toolResponseParts.Add(new
                            {
                                functionResponse = new
                                {
                                    name = fc.Name,
                                    response = new
                                    {
                                        name = fc.Name,
                                        content = ParseJsonOrObject(toolRes.ResultJson)
                                    }
                                }
                            });
                        }
                    }

                    // Add tool responses turn to geminiContents
                    geminiContents.Add(new
                    {
                        role = "user",
                        parts = toolResponseParts.ToArray()
                    });

                    // Continue loop so Gemini can interpret tool results
                    continue;
                }
                else
                {
                    // Text response received
                    finalAssistantReply = textParts.Count > 0 
                        ? string.Join("\n", textParts) 
                        : "I have processed your request. How else can I assist you today?";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply, null, partsElem.GetRawText()));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Google Gemini API.");
            providerErrorMessage = ex.Message;
            finalAssistantReply = $"⚠️ [Google Gemini Connection Error]: Could not communicate with Gemini API ({endpoint}): {ex.Message}";
            _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
        }

        finalAssistantReply ??= "I have processed your request. How else can I assist you with your framing order today?";

        return new AiChatResponseDto
        {
            ReplyMessage = finalAssistantReply,
            CustomerPhone = session.CustomerPhone,
            IsHumanTakeover = session.IsHumanTakeover,
            ToolsExecuted = toolsExecuted,
            OrderDraft = session.Draft,
            SessionId = session.SessionId,
            IsProviderConfigured = true,
            ProviderMessage = providerErrorMessage != null 
                ? $"Error from Google Gemini: {providerErrorMessage}" 
                : $"Completed via Google Gemini model '{model}'"
        };
    }

    /// <summary>
    /// Groq multi-turn function calling conversation loop using Groq OpenAI-compatible Chat Completions API.
    /// Endpoint: https://api.groq.com/openai/v1/chat/completions
    /// Authenticates securely via 'Authorization: Bearer <apiKey>' request header.
    /// </summary>
    private async Task<AiChatResponseDto> ExecuteGroqConversationLoopAsync(
        AiChatRequestDto request,
        AiChatSession session,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var toolsExecuted = new List<AiToolCallExecutionDto>();
        var client = _httpClientFactory.CreateClient("AiAssistantClient");

        var endpoint = !string.IsNullOrWhiteSpace(_settings.BaseUrl)
            ? _settings.BaseUrl.TrimEnd('/') + "/chat/completions"
            : "https://api.groq.com/openai/v1/chat/completions";

        var toolDefs = GetToolDefinitions();
        var systemPrompt = GetSystemPrompt();

        // Build messages payload
        var messagesPayload = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in session.Messages)
        {
            if (msg.Role == "tool")
            {
                messagesPayload.Add(new
                {
                    role = "tool",
                    tool_call_id = msg.ToolCallId,
                    name = msg.Name,
                    content = msg.Content
                });
            }
            else if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                messagesPayload.Add(new
                {
                    role = "assistant",
                    content = msg.Content,
                    tool_calls = msg.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new
                        {
                            name = tc.Function.Name,
                            arguments = tc.Function.Arguments
                        }
                    }).ToList()
                });
            }
            else
            {
                messagesPayload.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        // Multi-turn tool loop (up to 5 turns)
        var maxIterations = 5;
        string? finalAssistantReply = null;
        string? providerErrorMessage = null;

        try
        {
            for (int turn = 0; turn < maxIterations; turn++)
            {
                var reqBody = new
                {
                    model,
                    temperature = _settings.Temperature,
                    max_tokens = _settings.MaxTokens > 0 ? _settings.MaxTokens : 1500,
                    messages = messagesPayload,
                    tools = toolDefs.Select(t => new
                    {
                        type = "function",
                        function = new
                        {
                            name = t.Function.Name,
                            description = t.Function.Description,
                            parameters = t.Function.Parameters
                        }
                    }).ToList()
                };

                var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(reqBody, JsonOptions), Encoding.UTF8, "application/json")
                };
                httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var httpRes = await client.SendAsync(httpReq, cancellationToken);
                var resJson = await httpRes.Content.ReadAsStringAsync(cancellationToken);

                if (!httpRes.IsSuccessStatusCode)
                {
                    var statusCode = (int)httpRes.StatusCode;
                    string errorSummary = $"HTTP {statusCode} ({httpRes.StatusCode})";
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(resJson))
                        {
                            using var errDoc = JsonDocument.Parse(resJson);
                            if (errDoc.RootElement.TryGetProperty("error", out var errElem))
                            {
                                var msg = errElem.TryGetProperty("message", out var mProp) ? mProp.GetString() : null;
                                var errType = errElem.TryGetProperty("type", out var tProp) ? tProp.GetString() : null;
                                var errCode = errElem.TryGetProperty("code", out var cdProp) ? cdProp.GetString() : null;
                                errorSummary = $"{errType ?? errCode ?? "ERROR"}: {msg ?? "API Error"}";
                            }
                        }
                    }
                    catch
                    {
                        // fallback
                    }

                    _logger.LogError("Groq completion request failed with status {Status}: {ErrorSummary}", statusCode, errorSummary);
                    providerErrorMessage = errorSummary;

                    if (statusCode == 401 || statusCode == 403)
                    {
                        finalAssistantReply = $"⚠️ [Groq Authentication Error (HTTP {statusCode})]: {errorSummary}\n\n" +
                                             $"Please verify that your Groq API key in .NET User Secrets ('AiAssistant:GroqApiKey') is valid and authorized.";
                    }
                    else if (statusCode == 429)
                    {
                        finalAssistantReply = $"⚠️ [Groq Rate Limit (HTTP 429)]: {errorSummary}\n\n" +
                                             $"Groq request rate or token quota limit reached. Please wait a moment before sending another message.";
                    }
                    else if (statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
                    {
                        finalAssistantReply = $"⚠️ [Groq Service Error (HTTP {statusCode})]: The Groq AI service is temporarily unavailable. Please try again shortly.";
                    }
                    else
                    {
                        finalAssistantReply = $"⚠️ [Groq API Error (HTTP {statusCode})]: {errorSummary}";
                    }

                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }

                using var doc = JsonDocument.Parse(resJson);
                var choice = doc.RootElement.GetProperty("choices")[0];
                var messageElem = choice.GetProperty("message");

                var assistantContent = messageElem.TryGetProperty("content", out var cProp) ? cProp.GetString() : null;
                var hasToolCalls = messageElem.TryGetProperty("tool_calls", out var toolCallsElem) && toolCallsElem.ValueKind == JsonValueKind.Array;

                if (hasToolCalls)
                {
                    var toolCallsList = new List<AiToolCallInfo>();
                    foreach (var tc in toolCallsElem.EnumerateArray())
                    {
                        var tcId = tc.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                        var fnName = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                        var fnArgs = tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";

                        toolCallsList.Add(new AiToolCallInfo
                        {
                            Id = tcId,
                            Function = new AiFunctionCallDetail { Name = fnName, Arguments = fnArgs }
                        });
                    }

                    // Add assistant message with tool calls to session & payload
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(assistantContent, toolCallsList));
                    messagesPayload.Add(new
                    {
                        role = "assistant",
                        content = assistantContent,
                        tool_calls = toolCallsList.Select(tc => new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                        }).ToList()
                    });

                    // Execute each tool call against live PostgreSQL database via AiToolsService
                    foreach (var tc in toolCallsList)
                    {
                        var toolRes = await ExecuteToolCallAsync(tc.Function.Name, tc.Function.Arguments, session, cancellationToken);
                        toolsExecuted.Add(new AiToolCallExecutionDto
                        {
                            ToolName = tc.Function.Name,
                            ToolCallId = tc.Id,
                            Success = toolRes.Success,
                            ArgumentsJson = tc.Function.Arguments,
                            ResultJson = toolRes.ResultJson,
                            ErrorMessage = toolRes.ErrorMessage
                        });

                        // Add tool response to history & payload
                        _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Tool(toolRes.ResultJson, tc.Id, tc.Function.Name));
                        messagesPayload.Add(new
                        {
                            role = "tool",
                            tool_call_id = tc.Id,
                            name = tc.Function.Name,
                            content = toolRes.ResultJson
                        });
                    }

                    // Continue loop so Groq can read tool outputs and formulate response
                    continue;
                }
                else
                {
                    // Final textual response received
                    finalAssistantReply = assistantContent ?? "How else can I assist you with your framing order today?";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Groq API.");
            providerErrorMessage = ex.Message;
            finalAssistantReply = $"⚠️ [Groq Connection Error]: Could not communicate with Groq endpoint ({endpoint}): {ex.Message}";
            _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
        }

        finalAssistantReply ??= "I have processed your request. How else can I assist you today?";

        return new AiChatResponseDto
        {
            ReplyMessage = finalAssistantReply,
            CustomerPhone = session.CustomerPhone,
            IsHumanTakeover = session.IsHumanTakeover,
            ToolsExecuted = toolsExecuted,
            OrderDraft = session.Draft,
            SessionId = session.SessionId,
            IsProviderConfigured = true,
            ProviderMessage = providerErrorMessage != null 
                ? $"Error from Groq: {providerErrorMessage}" 
                : $"Completed via Groq model '{model}'"
        };
    }

    /// <summary>
    /// OpenAI-compatible multi-turn function calling conversation loop.
    /// Preserves full OpenAI compatibility for quick provider switching.
    /// </summary>
    private async Task<AiChatResponseDto> ExecuteOpenAiConversationLoopAsync(
        AiChatRequestDto request,
        AiChatSession session,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var toolsExecuted = new List<AiToolCallExecutionDto>();
        var client = _httpClientFactory.CreateClient("AiAssistantClient");

        var endpoint = !string.IsNullOrWhiteSpace(_settings.BaseUrl)
            ? _settings.BaseUrl.TrimEnd('/') + "/chat/completions"
            : "https://api.openai.com/v1/chat/completions";

        var toolDefs = GetToolDefinitions();
        var systemPrompt = GetSystemPrompt();

        // Build messages payload
        var messagesPayload = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in session.Messages)
        {
            if (msg.Role == "tool")
            {
                messagesPayload.Add(new
                {
                    role = "tool",
                    tool_call_id = msg.ToolCallId,
                    name = msg.Name,
                    content = msg.Content
                });
            }
            else if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                messagesPayload.Add(new
                {
                    role = "assistant",
                    content = msg.Content,
                    tool_calls = msg.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new
                        {
                            name = tc.Function.Name,
                            arguments = tc.Function.Arguments
                        }
                    }).ToList()
                });
            }
            else
            {
                messagesPayload.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        // Multi-turn tool loop (up to 5 turns)
        var maxIterations = 5;
        string? finalAssistantReply = null;
        string? providerErrorMessage = null;

        try
        {
            for (int turn = 0; turn < maxIterations; turn++)
            {
                var reqBody = new
                {
                    model,
                    temperature = _settings.Temperature,
                    max_tokens = _settings.MaxTokens > 0 ? _settings.MaxTokens : 1500,
                    messages = messagesPayload,
                    tools = toolDefs.Select(t => new
                    {
                        type = "function",
                        function = new
                        {
                            name = t.Function.Name,
                            description = t.Function.Description,
                            parameters = t.Function.Parameters
                        }
                    }).ToList()
                };

                var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(reqBody, JsonOptions), Encoding.UTF8, "application/json")
                };
                httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var httpRes = await client.SendAsync(httpReq, cancellationToken);
                var resJson = await httpRes.Content.ReadAsStringAsync(cancellationToken);

                if (!httpRes.IsSuccessStatusCode)
                {
                    string errorSummary = $"HTTP {(int)httpRes.StatusCode} ({httpRes.StatusCode})";
                    try
                    {
                        using var errDoc = JsonDocument.Parse(resJson);
                        if (errDoc.RootElement.TryGetProperty("error", out var errElem))
                        {
                            var msg = errElem.TryGetProperty("message", out var mProp) ? mProp.GetString() : null;
                            var errType = errElem.TryGetProperty("type", out var tProp) ? tProp.GetString() : null;
                            errorSummary = $"{errType ?? "ERROR"}: {msg ?? "API Error"}";
                        }
                    }
                    catch
                    {
                        // fallback
                    }

                    _logger.LogError("OpenAI completion request failed with status {Status}: {ErrorSummary}", httpRes.StatusCode, errorSummary);
                    providerErrorMessage = errorSummary;
                    finalAssistantReply = $"⚠️ [OpenAI API Error (HTTP {(int)httpRes.StatusCode})]: {errorSummary}\n\n" +
                                         $"Please verify that your configured API Key in appsettings.json or User Secrets is active and valid.";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }

                using var doc = JsonDocument.Parse(resJson);
                var choice = doc.RootElement.GetProperty("choices")[0];
                var messageElem = choice.GetProperty("message");

                var assistantContent = messageElem.TryGetProperty("content", out var cProp) ? cProp.GetString() : null;
                var hasToolCalls = messageElem.TryGetProperty("tool_calls", out var toolCallsElem) && toolCallsElem.ValueKind == JsonValueKind.Array;

                if (hasToolCalls)
                {
                    var toolCallsList = new List<AiToolCallInfo>();
                    foreach (var tc in toolCallsElem.EnumerateArray())
                    {
                        var tcId = tc.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N");
                        var fnName = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                        var fnArgs = tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";

                        toolCallsList.Add(new AiToolCallInfo
                        {
                            Id = tcId,
                            Function = new AiFunctionCallDetail { Name = fnName, Arguments = fnArgs }
                        });
                    }

                    // Add assistant message with tool calls to session
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(assistantContent, toolCallsList));
                    messagesPayload.Add(new
                    {
                        role = "assistant",
                        content = assistantContent,
                        tool_calls = toolCallsList.Select(tc => new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.Function.Name, arguments = tc.Function.Arguments }
                        }).ToList()
                    });

                    // Execute each tool call against real PostgreSQL database
                    foreach (var tc in toolCallsList)
                    {
                        var toolRes = await ExecuteToolCallAsync(tc.Function.Name, tc.Function.Arguments, session, cancellationToken);
                        toolsExecuted.Add(new AiToolCallExecutionDto
                        {
                            ToolName = tc.Function.Name,
                            ToolCallId = tc.Id,
                            Success = toolRes.Success,
                            ArgumentsJson = tc.Function.Arguments,
                            ResultJson = toolRes.ResultJson,
                            ErrorMessage = toolRes.ErrorMessage
                        });

                        // Add tool response to history & payload
                        _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Tool(toolRes.ResultJson, tc.Id, tc.Function.Name));
                        messagesPayload.Add(new
                        {
                            role = "tool",
                            tool_call_id = tc.Id,
                            name = tc.Function.Name,
                            content = toolRes.ResultJson
                        });
                    }

                    // Continue loop so LLM can read tool outputs and formulate reply
                    continue;
                }
                else
                {
                    // Final textual response received
                    finalAssistantReply = assistantContent ?? "How else can I assist you with your framing order today?";
                    _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with OpenAI API.");
            providerErrorMessage = ex.Message;
            finalAssistantReply = $"⚠️ [OpenAI Provider Connection Error]: Could not communicate with endpoint ({endpoint}): {ex.Message}";
            _sessionStore.AddMessage(session.CustomerPhone, AiChatMessage.Assistant(finalAssistantReply));
        }

        finalAssistantReply ??= "I have processed your request. How else can I assist you today?";

        return new AiChatResponseDto
        {
            ReplyMessage = finalAssistantReply,
            CustomerPhone = session.CustomerPhone,
            IsHumanTakeover = session.IsHumanTakeover,
            ToolsExecuted = toolsExecuted,
            OrderDraft = session.Draft,
            SessionId = session.SessionId,
            IsProviderConfigured = true,
            ProviderMessage = providerErrorMessage != null 
                ? $"Error from OpenAI: {providerErrorMessage}" 
                : $"Completed via OpenAI model '{model}'"
        };
    }

    private static string MapToJsonSchemaType(string? type)
    {
        return (type?.ToLowerInvariant()) switch
        {
            "string" => "STRING",
            "number" => "NUMBER",
            "integer" => "INTEGER",
            "boolean" => "BOOLEAN",
            "array" => "ARRAY",
            "object" => "OBJECT",
            _ => "STRING"
        };
    }

    private static object ParseJsonOrObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new { };
        }
        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return new { output = json };
        }
    }

    public Task<AiChatSession> GetSessionAsync(string customerPhone)
    {
        var session = _sessionStore.GetOrCreateSession(customerPhone);
        return Task.FromResult(session);
    }

    public Task<bool> ResetSessionAsync(string customerPhone)
    {
        var result = _sessionStore.ResetSession(customerPhone);
        return Task.FromResult(result);
    }

    public async Task<AiTransferToHumanResponseDto> EscalateToHumanAsync(
        string customerPhone,
        string customerName,
        string reason,
        string? channel = "WhatsApp")
    {
        var request = new AiTransferToHumanRequest
        {
            CustomerPhone = customerPhone,
            CustomerName = customerName,
            Reason = reason,
            Channel = channel ?? "WhatsApp",
            ChatSummary = $"Manual escalation requested for customer {customerName}"
        };

        var res = await _aiToolsService.TransferToHumanAsync(request);
        _sessionStore.SetHumanTakeover(customerPhone, true, reason);
        return res;
    }
}

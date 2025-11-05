package agent

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
	"time"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
)

// LLMClient defines the interface for LLM providers
type LLMClient interface {
	Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error)
}

// OpenAIClient implements LLM client for OpenAI
type OpenAIClient struct {
	apiKey string
	model  string
	client *http.Client
}

// AnthropicClient implements LLM client for Anthropic Claude
type AnthropicClient struct {
	apiKey string
	model  string
	client *http.Client
}

// GeminiClient implements LLM client for Google Gemini
type GeminiClient struct {
	apiKey string
	model  string
	client *http.Client
}

// OllamaClient implements LLM client for Ollama (local LLM)
type OllamaClient struct {
	baseURL string
	model   string
	client  *http.Client
}

// LMStudioClient implements LLM client for LM Studio (OpenAI-compatible)
type LMStudioClient struct {
	baseURL string
	model   string
	client  *http.Client
}

// NewLLMClient creates a new LLM client based on the provider
func NewLLMClient(config *models.LLMConfig) (LLMClient, error) {
	switch strings.ToLower(config.Provider) {
	case "openai":
		if config.APIKey == "" {
			return nil, fmt.Errorf("OpenAI API key is required")
		}
		model := config.Model
		if model == "" {
			model = "gpt-4-turbo-preview"
		}
		return &OpenAIClient{
			apiKey: config.APIKey,
			model:  model,
			client: &http.Client{Timeout: 30 * time.Second},
		}, nil
	case "anthropic", "claude":
		if config.APIKey == "" {
			return nil, fmt.Errorf("Anthropic API key is required")
		}
		model := config.Model
		if model == "" {
			model = "claude-3-5-sonnet-20241022"
		}
		return &AnthropicClient{
			apiKey: config.APIKey,
			model:  model,
			client: &http.Client{Timeout: 30 * time.Second},
		}, nil
	case "gemini", "google":
		if config.APIKey == "" {
			return nil, fmt.Errorf("Gemini API key is required")
		}
		model := config.Model
		if model == "" {
			model = "gemini-1.5-pro"
		}
		return &GeminiClient{
			apiKey: config.APIKey,
			model:  model,
			client: &http.Client{Timeout: 30 * time.Second},
		}, nil
	case "ollama":
		baseURL := config.BaseURL
		if baseURL == "" {
			baseURL = "http://localhost:11434"
		}
		model := config.Model
		if model == "" {
			model = "llama2"
		}
		return &OllamaClient{
			baseURL: baseURL,
			model:   model,
			client:  &http.Client{Timeout: 60 * time.Second}, // Longer timeout for local models
		}, nil
	case "lmstudio", "lm-studio":
		baseURL := config.BaseURL
		if baseURL == "" {
			baseURL = "http://localhost:1234"
		}
		model := config.Model
		if model == "" {
			model = "local-model"
		}
		return &LMStudioClient{
			baseURL: baseURL,
			model:   model,
			client:  &http.Client{Timeout: 60 * time.Second},
		}, nil
	default:
		return nil, fmt.Errorf("unsupported LLM provider: %s (supported: openai, anthropic, gemini, ollama, lmstudio)", config.Provider)
	}
}

// Analyze uses OpenAI to analyze the HTTP request/response
func (c *OpenAIClient) Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error) {
	systemPrompt := buildSystemPrompt()
	userPrompt := buildUserPrompt(request, response, prompt)

	reqBody := map[string]interface{}{
		"model": c.model,
		"messages": []map[string]string{
			{"role": "system", "content": systemPrompt},
			{"role": "user", "content": userPrompt},
		},
		"temperature": 0.7,
		"max_tokens":  1000,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequestWithContext(ctx, "POST", "https://api.openai.com/v1/chat/completions", bytes.NewBuffer(jsonData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Authorization", "Bearer "+c.apiKey)

	resp, err := c.client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to call OpenAI API: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("OpenAI API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result struct {
		Choices []struct {
			Message struct {
				Content string `json:"content"`
			} `json:"message"`
		} `json:"choices"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Choices) == 0 {
		return "", fmt.Errorf("no response from OpenAI")
	}

	return result.Choices[0].Message.Content, nil
}

// Analyze uses Anthropic Claude to analyze the HTTP request/response
func (c *AnthropicClient) Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error) {
	systemPrompt := buildSystemPrompt()
	userPrompt := buildUserPrompt(request, response, prompt)

	reqBody := map[string]interface{}{
		"model": c.model,
		"max_tokens": 1024,
		"system": systemPrompt,
		"messages": []map[string]string{
			{"role": "user", "content": userPrompt},
		},
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	req, err := http.NewRequestWithContext(ctx, "POST", "https://api.anthropic.com/v1/messages", bytes.NewBuffer(jsonData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("x-api-key", c.apiKey)
	req.Header.Set("anthropic-version", "2023-06-01")

	resp, err := c.client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to call Anthropic API: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("Anthropic API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result struct {
		Content []struct {
			Text string `json:"text"`
		} `json:"content"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Content) == 0 {
		return "", fmt.Errorf("no response from Anthropic")
	}

	return result.Content[0].Text, nil
}

// Analyze uses Google Gemini to analyze the HTTP request/response
func (c *GeminiClient) Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error) {
	systemPrompt := buildSystemPrompt()
	userPrompt := buildUserPrompt(request, response, prompt)

	// Gemini uses a different request structure
	reqBody := map[string]interface{}{
		"contents": []map[string]interface{}{
			{
				"parts": []map[string]string{
					{"text": systemPrompt + "\n\n" + userPrompt},
				},
			},
		},
		"generationConfig": map[string]interface{}{
			"temperature":     0.7,
			"maxOutputTokens": 1024,
		},
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	// Gemini API URL includes the API key as a query parameter
	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/%s:generateContent?key=%s", c.model, c.apiKey)

	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")

	resp, err := c.client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to call Gemini API: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("Gemini API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result struct {
		Candidates []struct {
			Content struct {
				Parts []struct {
					Text string `json:"text"`
				} `json:"parts"`
			} `json:"content"`
		} `json:"candidates"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Candidates) == 0 || len(result.Candidates[0].Content.Parts) == 0 {
		return "", fmt.Errorf("no response from Gemini")
	}

	return result.Candidates[0].Content.Parts[0].Text, nil
}

// Analyze uses Ollama to analyze the HTTP request/response
func (c *OllamaClient) Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error) {
	systemPrompt := buildSystemPrompt()
	userPrompt := buildUserPrompt(request, response, prompt)

	reqBody := map[string]interface{}{
		"model":  c.model,
		"prompt": systemPrompt + "\n\n" + userPrompt,
		"stream": false,
		"options": map[string]interface{}{
			"temperature": 0.7,
		},
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	url := c.baseURL + "/api/generate"
	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")

	resp, err := c.client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to call Ollama API: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("Ollama API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result struct {
		Response string `json:"response"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if result.Response == "" {
		return "", fmt.Errorf("no response from Ollama")
	}

	return result.Response, nil
}

// Analyze uses LM Studio to analyze the HTTP request/response
// LM Studio uses OpenAI-compatible API
func (c *LMStudioClient) Analyze(ctx context.Context, request *models.RequestConfig, response *models.Response, prompt string) (string, error) {
	systemPrompt := buildSystemPrompt()
	userPrompt := buildUserPrompt(request, response, prompt)

	reqBody := map[string]interface{}{
		"model": c.model,
		"messages": []map[string]string{
			{"role": "system", "content": systemPrompt},
			{"role": "user", "content": userPrompt},
		},
		"temperature": 0.7,
		"max_tokens":  1000,
	}

	jsonData, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	url := c.baseURL + "/v1/chat/completions"
	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")

	resp, err := c.client.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to call LM Studio API: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("LM Studio API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result struct {
		Choices []struct {
			Message struct {
				Content string `json:"content"`
			} `json:"message"`
		} `json:"choices"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Choices) == 0 {
		return "", fmt.Errorf("no response from LM Studio")
	}

	return result.Choices[0].Message.Content, nil
}

// buildSystemPrompt creates the system prompt for the LLM
func buildSystemPrompt() string {
	return `You are an intelligent HTTP debugging and analysis assistant. Your role is to help users understand HTTP requests and responses.

When analyzing requests and responses:
- Provide clear, concise answers in natural language
- Format JSON responses with proper indentation
- Explain HTTP status codes and their meanings
- Identify common issues (CORS, authentication, rate limits, etc.)
- Suggest improvements when appropriate
- Use simple terms for technical concepts
- Be specific about what you observe in the data

If the user asks about the status code, explain what it means.
If they ask if a URL is accessible, check the status code (2xx = success, 4xx = client error, 5xx = server error).
If they ask about timing, provide context (< 100ms = fast, 100-500ms = moderate, > 500ms = slow).
If they ask about content, format it nicely and highlight key information.`
}

// buildUserPrompt creates the user prompt with request/response details
func buildUserPrompt(request *models.RequestConfig, response *models.Response, userQuestion string) string {
	var sb strings.Builder

	sb.WriteString("HTTP Request and Response Analysis:\n\n")
	sb.WriteString(fmt.Sprintf("Request:\n"))
	sb.WriteString(fmt.Sprintf("- Method: %s\n", request.Method))
	sb.WriteString(fmt.Sprintf("- URL: %s\n", request.URL))

	if len(request.Headers) > 0 {
		sb.WriteString("- Headers:\n")
		for k, v := range request.Headers {
			sb.WriteString(fmt.Sprintf("  %s: %s\n", k, v))
		}
	}

	if request.Body != "" {
		bodyPreview := request.Body
		if len(bodyPreview) > 500 {
			bodyPreview = bodyPreview[:500] + "... (truncated)"
		}
		sb.WriteString(fmt.Sprintf("- Body: %s\n", bodyPreview))
	}

	sb.WriteString(fmt.Sprintf("\nResponse:\n"))
	sb.WriteString(fmt.Sprintf("- Status: %d %s\n", response.StatusCode, response.Status))
	sb.WriteString(fmt.Sprintf("- Duration: %s\n", FormatDuration(response.Duration)))
	sb.WriteString(fmt.Sprintf("- Content-Type: %s\n", response.ContentType))
	sb.WriteString(fmt.Sprintf("- Content-Length: %d bytes\n", response.ContentLength))

	if len(response.Headers) > 0 {
		sb.WriteString("- Response Headers:\n")
		for k, v := range response.Headers {
			sb.WriteString(fmt.Sprintf("  %s: %s\n", k, strings.Join(v, ", ")))
		}
	}

	if response.Body != "" {
		bodyPreview := response.Body
		if len(bodyPreview) > 1000 {
			bodyPreview = bodyPreview[:1000] + "... (truncated)"
		}
		sb.WriteString(fmt.Sprintf("- Response Body:\n%s\n", bodyPreview))
	}

	// Add user question
	if userQuestion == "" {
		userQuestion = "What is the status code of this request?"
	}
	sb.WriteString(fmt.Sprintf("\nUser Question: %s\n", userQuestion))
	sb.WriteString("\nProvide a clear and helpful answer:")

	return sb.String()
}

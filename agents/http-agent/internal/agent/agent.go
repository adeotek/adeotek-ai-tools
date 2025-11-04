package agent

import (
	"context"
	"encoding/json"
	"fmt"
	"strings"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
)

// HTTPAgent combines HTTP client and LLM for intelligent request analysis
type HTTPAgent struct {
	httpClient *HTTPClient
	llmClient  LLMClient
}

// NewHTTPAgent creates a new HTTP agent
func NewHTTPAgent(httpConfig *models.HTTPConfig, llmConfig *models.LLMConfig) (*HTTPAgent, error) {
	httpClient := NewHTTPClient(httpConfig)

	llmClient, err := NewLLMClient(llmConfig)
	if err != nil {
		return nil, fmt.Errorf("failed to create LLM client: %w", err)
	}

	return &HTTPAgent{
		httpClient: httpClient,
		llmClient:  llmClient,
	}, nil
}

// Execute performs an HTTP request and analyzes it with AI
func (a *HTTPAgent) Execute(ctx context.Context, reqConfig *models.RequestConfig) (*models.AnalysisResult, error) {
	// Make the HTTP request
	response, err := a.httpClient.MakeRequest(ctx, reqConfig)
	if err != nil {
		return &models.AnalysisResult{
			Request:  reqConfig,
			Response: nil,
			Error:    err.Error(),
		}, nil
	}

	// Format the response body if it's JSON
	formattedBody := formatResponseBody(response)

	// Analyze with LLM
	analysis, err := a.llmClient.Analyze(ctx, reqConfig, response, reqConfig.Prompt)
	if err != nil {
		// Return the response even if analysis fails
		analysis = fmt.Sprintf("Analysis unavailable: %v\n\nBasic Info: Request returned %d %s in %s",
			err, response.StatusCode, response.Status, FormatDuration(response.Duration))
	}

	result := &models.AnalysisResult{
		Request:         reqConfig,
		Response:        response,
		Analysis:        analysis,
		FormattedBody:   formattedBody,
		RequestDuration: FormatDuration(response.Duration),
	}

	return result, nil
}

// formatResponseBody attempts to pretty-print JSON response bodies
func formatResponseBody(response *models.Response) string {
	if response == nil || response.Body == "" {
		return ""
	}

	contentType := strings.ToLower(response.ContentType)

	// Try to format JSON
	if strings.Contains(contentType, "json") {
		var jsonData interface{}
		if err := json.Unmarshal([]byte(response.Body), &jsonData); err == nil {
			formatted, err := json.MarshalIndent(jsonData, "", "  ")
			if err == nil {
				return string(formatted)
			}
		}
	}

	// For other content types, return as-is
	return response.Body
}

// GetStatusCodeColor returns a color class for the status code
func GetStatusCodeColor(statusCode int) string {
	switch {
	case statusCode >= 200 && statusCode < 300:
		return "success"
	case statusCode >= 300 && statusCode < 400:
		return "info"
	case statusCode >= 400 && statusCode < 500:
		return "warning"
	case statusCode >= 500:
		return "error"
	default:
		return "default"
	}
}

// GetStatusCodeDescription returns a human-readable description of the status code
func GetStatusCodeDescription(statusCode int) string {
	descriptions := map[int]string{
		200: "OK - Request succeeded",
		201: "Created - Resource created successfully",
		204: "No Content - Request succeeded with no response body",
		301: "Moved Permanently - Resource has been moved",
		302: "Found - Temporary redirect",
		304: "Not Modified - Cached version is still valid",
		400: "Bad Request - Invalid request syntax",
		401: "Unauthorized - Authentication required",
		403: "Forbidden - Access denied",
		404: "Not Found - Resource doesn't exist",
		405: "Method Not Allowed - HTTP method not supported",
		429: "Too Many Requests - Rate limit exceeded",
		500: "Internal Server Error - Server encountered an error",
		502: "Bad Gateway - Invalid response from upstream server",
		503: "Service Unavailable - Server temporarily unavailable",
		504: "Gateway Timeout - Upstream server didn't respond in time",
	}

	if desc, ok := descriptions[statusCode]; ok {
		return desc
	}

	// Generic descriptions for ranges
	switch {
	case statusCode >= 200 && statusCode < 300:
		return "Success"
	case statusCode >= 300 && statusCode < 400:
		return "Redirection"
	case statusCode >= 400 && statusCode < 500:
		return "Client Error"
	case statusCode >= 500:
		return "Server Error"
	default:
		return "Unknown Status"
	}
}

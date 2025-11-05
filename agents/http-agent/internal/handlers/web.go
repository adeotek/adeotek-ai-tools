package handlers

import (
	"embed"
	"html/template"
	"io/fs"
	"log"
	"net/http"
	"strings"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/agent"
	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
	"github.com/gin-gonic/gin"
)

//go:embed templates/*
var templatesFS embed.FS

//go:embed static/*
var staticFS embed.FS

// Handler handles HTTP requests
type Handler struct {
	agent *agent.HTTPAgent
}

// NewHandler creates a new handler
func NewHandler(ag *agent.HTTPAgent) *Handler {
	return &Handler{agent: ag}
}

// SetupRoutes configures the Gin routes
func (h *Handler) SetupRoutes(r *gin.Engine) {
	// Load templates
	tmpl, err := template.ParseFS(templatesFS, "templates/*.html")
	if err != nil {
		log.Fatalf("Failed to parse templates: %v", err)
	}
	r.SetHTMLTemplate(tmpl)

	// Serve static files - strip "static" prefix from embedded FS
	staticSub, err := fs.Sub(staticFS, "static")
	if err != nil {
		log.Fatalf("Failed to create static sub-filesystem: %v", err)
	}
	r.StaticFS("/static", http.FS(staticSub))

	// Routes
	r.GET("/", h.handleIndex)
	r.POST("/api/request", h.handleRequest)
	r.GET("/health", h.handleHealth)
}

// handleIndex serves the main page
func (h *Handler) handleIndex(c *gin.Context) {
	c.HTML(http.StatusOK, "index.html", gin.H{
		"title": "Intelligent HTTP Agent",
	})
}

// handleRequest processes HTTP request and returns analysis
func (h *Handler) handleRequest(c *gin.Context) {
	var req models.RequestConfig
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{
			"error": "Invalid request format: " + err.Error(),
		})
		return
	}

	// Validate required fields
	if req.URL == "" {
		c.JSON(http.StatusBadRequest, gin.H{
			"error": "URL is required",
		})
		return
	}

	if req.Method == "" {
		req.Method = "GET"
	}

	// Normalize method
	req.Method = strings.ToUpper(req.Method)

	// Execute request
	result, err := h.agent.Execute(c.Request.Context(), &req)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{
			"error": "Failed to execute request: " + err.Error(),
		})
		return
	}

	// Add color and description for status code
	if result.Response != nil {
		c.JSON(http.StatusOK, gin.H{
			"request":          result.Request,
			"response":         result.Response,
			"analysis":         result.Analysis,
			"formatted_body":   result.FormattedBody,
			"request_duration": result.RequestDuration,
			"status_color":     agent.GetStatusCodeColor(result.Response.StatusCode),
			"status_desc":      agent.GetStatusCodeDescription(result.Response.StatusCode),
			"dns_diagnostics":  result.DNSDiagnostics,
			"ssl_diagnostics":  result.SSLDiagnostics,
			"ssl_verified":     result.SSLVerified,
			"error":            result.Error,
		})
	} else {
		c.JSON(http.StatusOK, gin.H{
			"error":           result.Error,
			"dns_diagnostics": result.DNSDiagnostics,
			"ssl_diagnostics": result.SSLDiagnostics,
			"ssl_verified":    result.SSLVerified,
		})
	}
}

// handleHealth returns health status
func (h *Handler) handleHealth(c *gin.Context) {
	c.JSON(http.StatusOK, gin.H{
		"status": "healthy",
		"service": "http-agent",
	})
}

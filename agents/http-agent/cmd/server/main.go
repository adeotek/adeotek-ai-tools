package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/agent"
	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/handlers"
	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
	"github.com/gin-gonic/gin"
	"github.com/spf13/viper"
	"github.com/subosito/gotenv"
)

func main() {
	// Load configuration
	config, err := loadConfig()
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Create HTTP agent
	httpAgent, err := agent.NewHTTPAgent(&config.HTTP, &config.LLM)
	if err != nil {
		log.Fatalf("Failed to create HTTP agent: %v", err)
	}

	// Setup Gin
	if os.Getenv("GIN_MODE") == "" {
		gin.SetMode(gin.ReleaseMode)
	}
	router := gin.Default()

	// Setup handlers
	h := handlers.NewHandler(httpAgent)
	h.SetupRoutes(router)

	// Create server
	addr := fmt.Sprintf("%s:%s", config.Server.Host, config.Server.Port)
	srv := &http.Server{
		Addr:         addr,
		Handler:      router,
		ReadTimeout:  time.Duration(config.Server.ReadTimeout) * time.Second,
		WriteTimeout: time.Duration(config.Server.WriteTimeout) * time.Second,
	}

	// Start server in a goroutine
	go func() {
		log.Printf("Starting HTTP Agent on %s", addr)
		log.Printf("LLM Provider: %s (Model: %s)", config.LLM.Provider, config.LLM.Model)
		log.Printf("Open http://localhost:%s in your browser", config.Server.Port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("Failed to start server: %v", err)
		}
	}()

	// Wait for interrupt signal to gracefully shutdown the server
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit
	log.Println("Shutting down server...")

	// Graceful shutdown with 5 second timeout
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	if err := srv.Shutdown(ctx); err != nil {
		log.Fatal("Server forced to shutdown:", err)
	}

	log.Println("Server exited")
}

func loadConfig() (*models.Config, error) {
	// Load .env file if it exists (for local development)
	_ = gotenv.Load(".env")

	// Set default values
	viper.SetDefault("server.port", "8080")
	viper.SetDefault("server.host", "0.0.0.0")
	viper.SetDefault("server.read_timeout", 30)
	viper.SetDefault("server.write_timeout", 30)

	viper.SetDefault("llm.provider", "openai")
	viper.SetDefault("llm.model", "gpt-4-turbo-preview")

	viper.SetDefault("http.timeout", 30)
	viper.SetDefault("http.follow_redirects", true)
	viper.SetDefault("http.max_redirects", 10)
	viper.SetDefault("http.verify_ssl", false)
	viper.SetDefault("http.max_response_size", 10485760) // 10MB
	viper.SetDefault("http.block_private_ips", false)

	// Config file
	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.AddConfigPath("./config")
	viper.AddConfigPath(".")

	// Read config file (optional)
	if err := viper.ReadInConfig(); err != nil {
		if _, ok := err.(viper.ConfigFileNotFoundError); !ok {
			return nil, err
		}
	}

	// Environment variables
	viper.SetEnvPrefix("HTTP_AGENT")
	viper.AutomaticEnv()

	// Bind specific environment variables
	viper.BindEnv("server.port", "PORT")
	viper.BindEnv("llm.provider", "LLM_PROVIDER")
	viper.BindEnv("llm.api_key", "LLM_API_KEY", "OPENAI_API_KEY", "ANTHROPIC_API_KEY", "GEMINI_API_KEY", "GOOGLE_API_KEY")
	viper.BindEnv("llm.model", "LLM_MODEL")
	viper.BindEnv("llm.base_url", "HTTP_AGENT_LLM_BASE_URL")
	viper.BindEnv("llm.model", "LLM_MODEL")
	viper.BindEnv("http.timeout", "HTTP_TIMEOUT")
	viper.BindEnv("http.verify_ssl", "VERIFY_SSL")
	viper.BindEnv("http.block_private_ips", "BLOCK_PRIVATE_IPS")

	var config models.Config
	if err := viper.Unmarshal(&config); err != nil {
		return nil, err
	}

	// Validate required fields - API key needed for cloud providers only
	provider := strings.ToLower(config.LLM.Provider)
	requiresAPIKey := provider == "openai" || provider == "anthropic" || provider == "claude" || provider == "gemini" || provider == "google"

	// Local providers (ollama, lmstudio) don't require API keys
	if !requiresAPIKey {
		log.Printf("Using local LLM provider: %s (no API key required)", config.LLM.Provider)
		return &config, nil
	}

	// For cloud providers, validate API key
	if config.LLM.APIKey == "" {
		// Try to get from specific env vars based on provider
		switch provider {
		case "openai":
			if key := os.Getenv("OPENAI_API_KEY"); key != "" {
				config.LLM.APIKey = key
			} else {
				return nil, fmt.Errorf("provider '%s' requires an API key. Set OPENAI_API_KEY environment variable", config.LLM.Provider)
			}
		case "anthropic", "claude":
			if key := os.Getenv("ANTHROPIC_API_KEY"); key != "" {
				config.LLM.APIKey = key
			} else {
				return nil, fmt.Errorf("provider '%s' requires an API key. Set ANTHROPIC_API_KEY environment variable", config.LLM.Provider)
			}
		case "gemini", "google":
			if key := os.Getenv("GEMINI_API_KEY"); key != "" {
				config.LLM.APIKey = key
			} else if key := os.Getenv("GOOGLE_API_KEY"); key != "" {
				config.LLM.APIKey = key
			} else {
				return nil, fmt.Errorf("provider '%s' requires an API key. Set GEMINI_API_KEY or GOOGLE_API_KEY environment variable", config.LLM.Provider)
			}
		default:
			return nil, fmt.Errorf("unknown cloud provider '%s'. Supported: openai, anthropic, gemini, ollama, lmstudio", config.LLM.Provider)
		}
	}

	return &config, nil
}

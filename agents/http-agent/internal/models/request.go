package models

import "time"

// RequestConfig represents the configuration for an HTTP request
type RequestConfig struct {
	URL       string            `json:"url" binding:"required"`
	Method    string            `json:"method" binding:"required"`
	Headers   map[string]string `json:"headers"`
	Body      string            `json:"body"`
	Prompt    string            `json:"prompt"`
	VerifySSL *bool             `json:"verify_ssl"` // Optional, nil means use default
}

// Response represents an HTTP response with metadata
type Response struct {
	StatusCode    int                 `json:"status_code"`
	Status        string              `json:"status"`
	Headers       map[string][]string `json:"headers"`
	Body          string              `json:"body"`
	Duration      time.Duration       `json:"duration"`
	ContentType   string              `json:"content_type"`
	ContentLength int64               `json:"content_length"`
	Timestamp     time.Time           `json:"timestamp"`
}

// DNSDiagnostics contains DNS resolution information
type DNSDiagnostics struct {
	Hostname    string   `json:"hostname"`
	IPAddresses []string `json:"ip_addresses"`
	Error       string   `json:"error,omitempty"`
	LookupTime  string   `json:"lookup_time"`
}

// SSLCertificateDiagnostics contains SSL/TLS certificate information
type SSLCertificateDiagnostics struct {
	Present         bool      `json:"present"`
	Valid           bool      `json:"valid"`
	Subject         string    `json:"subject"`
	Issuer          string    `json:"issuer"`
	NotBefore       time.Time `json:"not_before"`
	NotAfter        time.Time `json:"not_after"`
	ExpiresIn       string    `json:"expires_in"`
	DNSNames        []string  `json:"dns_names"`
	SignatureAlgo   string    `json:"signature_algorithm"`
	PublicKeyAlgo   string    `json:"public_key_algorithm"`
	Version         int       `json:"version"`
	SerialNumber    string    `json:"serial_number"`
	Error           string    `json:"error,omitempty"`
	CertificateInfo string    `json:"certificate_info,omitempty"`
}

// AnalysisResult contains the AI-generated analysis of the request/response
type AnalysisResult struct {
	Request          *RequestConfig             `json:"request"`
	Response         *Response                  `json:"response"`
	Analysis         string                     `json:"analysis"`
	FormattedBody    string                     `json:"formatted_body,omitempty"`
	Error            string                     `json:"error,omitempty"`
	RequestDuration  string                     `json:"request_duration"`
	DNSDiagnostics   *DNSDiagnostics            `json:"dns_diagnostics,omitempty"`
	SSLDiagnostics   *SSLCertificateDiagnostics `json:"ssl_diagnostics,omitempty"`
	SSLVerified      bool                       `json:"ssl_verified"`
}

// Config represents the application configuration
type Config struct {
	Server ServerConfig `mapstructure:"server"`
	LLM    LLMConfig    `mapstructure:"llm"`
	HTTP   HTTPConfig   `mapstructure:"http"`
}

// ServerConfig holds server-specific settings
type ServerConfig struct {
	Port         string `mapstructure:"port"`
	Host         string `mapstructure:"host"`
	ReadTimeout  int    `mapstructure:"read_timeout"`
	WriteTimeout int    `mapstructure:"write_timeout"`
}

// LLMConfig holds LLM provider configuration
type LLMConfig struct {
	Provider string `mapstructure:"provider"` // openai, anthropic, ollama
	APIKey   string `mapstructure:"api_key"`
	Model    string `mapstructure:"model"`
	BaseURL  string `mapstructure:"base_url"` // For Ollama
}

// HTTPConfig holds HTTP client configuration
type HTTPConfig struct {
	Timeout          int  `mapstructure:"timeout"`
	FollowRedirects  bool `mapstructure:"follow_redirects"`
	MaxRedirects     int  `mapstructure:"max_redirects"`
	VerifySSL        bool `mapstructure:"verify_ssl"`
	MaxResponseSize  int  `mapstructure:"max_response_size"`
	BlockPrivateIPs  bool `mapstructure:"block_private_ips"`
}

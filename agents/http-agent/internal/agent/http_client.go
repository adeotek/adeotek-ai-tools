package agent

import (
	"bytes"
	"context"
	"crypto/tls"
	"fmt"
	"io"
	"net"
	"net/http"
	"net/url"
	"time"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
)

// HTTPClient handles HTTP request execution
type HTTPClient struct {
	client          *http.Client
	config          *models.HTTPConfig
	maxResponseSize int64
	blockPrivateIPs bool
}

// NewHTTPClient creates a new HTTP client with the given configuration
func NewHTTPClient(config *models.HTTPConfig) *HTTPClient {
	transport := &http.Transport{
		TLSClientConfig: &tls.Config{
			InsecureSkipVerify: !config.VerifySSL,
		},
		DialContext: func(ctx context.Context, network, addr string) (net.Conn, error) {
			dialer := &net.Dialer{
				Timeout:   time.Duration(config.Timeout) * time.Second,
				KeepAlive: 30 * time.Second,
			}

			// Block private IPs if configured
			if config.BlockPrivateIPs {
				host, _, err := net.SplitHostPort(addr)
				if err != nil {
					host = addr
				}

				if isPrivateIP(host) {
					return nil, fmt.Errorf("access to private IP addresses is blocked")
				}
			}

			return dialer.DialContext(ctx, network, addr)
		},
	}

	client := &http.Client{
		Transport: transport,
		Timeout:   time.Duration(config.Timeout) * time.Second,
	}

	if !config.FollowRedirects {
		client.CheckRedirect = func(req *http.Request, via []*http.Request) error {
			return http.ErrUseLastResponse
		}
	} else if config.MaxRedirects > 0 {
		client.CheckRedirect = func(req *http.Request, via []*http.Request) error {
			if len(via) >= config.MaxRedirects {
				return fmt.Errorf("stopped after %d redirects", config.MaxRedirects)
			}
			return nil
		}
	}

	maxSize := int64(10 * 1024 * 1024) // 10MB default
	if config.MaxResponseSize > 0 {
		maxSize = int64(config.MaxResponseSize)
	}

	return &HTTPClient{
		client:          client,
		config:          config,
		maxResponseSize: maxSize,
		blockPrivateIPs: config.BlockPrivateIPs,
	}
}

// MakeRequest executes an HTTP request and returns the response
func (c *HTTPClient) MakeRequest(ctx context.Context, reqConfig *models.RequestConfig) (*models.Response, error) {
	startTime := time.Now()

	// Validate URL
	if err := c.validateURL(reqConfig.URL); err != nil {
		return nil, fmt.Errorf("invalid URL: %w", err)
	}

	// Create request
	var bodyReader io.Reader
	if reqConfig.Body != "" {
		bodyReader = bytes.NewBufferString(reqConfig.Body)
	}

	req, err := http.NewRequestWithContext(ctx, reqConfig.Method, reqConfig.URL, bodyReader)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}

	// Add headers
	for key, value := range reqConfig.Headers {
		req.Header.Set(key, value)
	}

	// Set default User-Agent if not provided
	if req.Header.Get("User-Agent") == "" {
		req.Header.Set("User-Agent", "Intelligent-HTTP-Agent/1.0")
	}

	// Execute request
	resp, err := c.client.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to execute request: %w", err)
	}
	defer resp.Body.Close()

	// Read response body with size limit
	limitedReader := io.LimitReader(resp.Body, c.maxResponseSize)
	bodyBytes, err := io.ReadAll(limitedReader)
	if err != nil {
		return nil, fmt.Errorf("failed to read response body: %w", err)
	}

	duration := time.Since(startTime)

	// Build response
	response := &models.Response{
		StatusCode:    resp.StatusCode,
		Status:        resp.Status,
		Headers:       resp.Header,
		Body:          string(bodyBytes),
		Duration:      duration,
		ContentType:   resp.Header.Get("Content-Type"),
		ContentLength: resp.ContentLength,
		Timestamp:     startTime,
	}

	return response, nil
}

// validateURL validates and sanitizes the URL
func (c *HTTPClient) validateURL(rawURL string) error {
	parsedURL, err := url.Parse(rawURL)
	if err != nil {
		return fmt.Errorf("malformed URL: %w", err)
	}

	// Ensure scheme is http or https
	if parsedURL.Scheme != "http" && parsedURL.Scheme != "https" {
		return fmt.Errorf("only http and https schemes are allowed")
	}

	// Block private IPs if configured
	if c.blockPrivateIPs {
		host := parsedURL.Hostname()
		if isPrivateIP(host) {
			return fmt.Errorf("access to private IP addresses is blocked")
		}
	}

	return nil
}

// isPrivateIP checks if the given host is a private IP address
func isPrivateIP(host string) bool {
	// Check for localhost
	if host == "localhost" || host == "127.0.0.1" || host == "::1" {
		return true
	}

	ip := net.ParseIP(host)
	if ip == nil {
		// Try to resolve hostname
		ips, err := net.LookupIP(host)
		if err != nil || len(ips) == 0 {
			return false
		}
		ip = ips[0]
	}

	// Check for private IP ranges
	privateRanges := []string{
		"10.0.0.0/8",
		"172.16.0.0/12",
		"192.168.0.0/16",
		"169.254.0.0/16",
		"127.0.0.0/8",
		"fc00::/7",
		"fe80::/10",
	}

	for _, cidr := range privateRanges {
		_, subnet, err := net.ParseCIDR(cidr)
		if err != nil {
			continue
		}
		if subnet.Contains(ip) {
			return true
		}
	}

	return false
}

// FormatDuration returns a human-readable duration string
func FormatDuration(d time.Duration) string {
	if d < time.Millisecond {
		return fmt.Sprintf("%.2fμs", float64(d.Microseconds()))
	} else if d < time.Second {
		return fmt.Sprintf("%.2fms", float64(d.Milliseconds()))
	}
	return fmt.Sprintf("%.2fs", d.Seconds())
}

package agent

import (
	"crypto/tls"
	"crypto/x509"
	"fmt"
	"net"
	"net/url"
	"strings"
	"time"

	"github.com/adeotek/adeotek-ai-tools/agents/http-agent/internal/models"
)

// PerformDNSDiagnostics performs DNS lookup for the given URL
func PerformDNSDiagnostics(rawURL string) *models.DNSDiagnostics {
	startTime := time.Now()
	diag := &models.DNSDiagnostics{}

	// Parse URL to get hostname
	parsedURL, err := url.Parse(rawURL)
	if err != nil {
		diag.Error = fmt.Sprintf("Failed to parse URL: %v", err)
		diag.LookupTime = FormatDuration(time.Since(startTime))
		return diag
	}

	hostname := parsedURL.Hostname()
	diag.Hostname = hostname

	if hostname == "" {
		diag.Error = "No hostname found in URL"
		diag.LookupTime = FormatDuration(time.Since(startTime))
		return diag
	}

	// Perform DNS lookup
	ips, err := net.LookupIP(hostname)
	if err != nil {
		diag.Error = fmt.Sprintf("DNS lookup failed: %v", err)
		diag.LookupTime = FormatDuration(time.Since(startTime))
		return diag
	}

	// Convert IPs to strings
	for _, ip := range ips {
		diag.IPAddresses = append(diag.IPAddresses, ip.String())
	}

	diag.LookupTime = FormatDuration(time.Since(startTime))
	return diag
}

// PerformSSLDiagnostics performs SSL/TLS certificate inspection
func PerformSSLDiagnostics(rawURL string) *models.SSLCertificateDiagnostics {
	diag := &models.SSLCertificateDiagnostics{
		Present: false,
	}

	// Parse URL
	parsedURL, err := url.Parse(rawURL)
	if err != nil {
		diag.Error = fmt.Sprintf("Failed to parse URL: %v", err)
		return diag
	}

	// Only check SSL for HTTPS URLs
	if parsedURL.Scheme != "https" {
		diag.Error = "Not an HTTPS URL - no SSL certificate to check"
		return diag
	}

	hostname := parsedURL.Hostname()
	port := parsedURL.Port()
	if port == "" {
		port = "443"
	}

	// Connect and get certificate
	address := net.JoinHostPort(hostname, port)
	conn, err := tls.DialWithDialer(
		&net.Dialer{Timeout: 10 * time.Second},
		"tcp",
		address,
		&tls.Config{
			InsecureSkipVerify: false, // We want to check the cert validity
			ServerName:         hostname,
		},
	)
	if err != nil {
		// Try to get more specific error information
		if strings.Contains(err.Error(), "certificate") {
			diag.Present = true
			diag.Valid = false
			diag.Error = fmt.Sprintf("SSL certificate error: %v", err)
		} else {
			diag.Error = fmt.Sprintf("Failed to connect: %v", err)
		}
		return diag
	}
	defer conn.Close()

	// Get certificate chain
	certs := conn.ConnectionState().PeerCertificates
	if len(certs) == 0 {
		diag.Error = "No certificates received"
		return diag
	}

	// Analyze the leaf certificate (first in chain)
	cert := certs[0]
	diag.Present = true
	diag.Valid = true // If we got here without error, cert is valid

	// Extract certificate details
	diag.Subject = cert.Subject.String()
	diag.Issuer = cert.Issuer.String()
	diag.NotBefore = cert.NotBefore
	diag.NotAfter = cert.NotAfter
	diag.DNSNames = cert.DNSNames
	diag.SignatureAlgo = cert.SignatureAlgorithm.String()
	diag.PublicKeyAlgo = cert.PublicKeyAlgorithm.String()
	diag.Version = cert.Version
	diag.SerialNumber = cert.SerialNumber.String()

	// Calculate expiration time
	now := time.Now()
	if now.After(cert.NotAfter) {
		diag.Valid = false
		diag.ExpiresIn = "EXPIRED"
	} else if now.Before(cert.NotBefore) {
		diag.Valid = false
		diag.ExpiresIn = "NOT YET VALID"
	} else {
		duration := cert.NotAfter.Sub(now)
		days := int(duration.Hours() / 24)
		if days > 0 {
			diag.ExpiresIn = fmt.Sprintf("%d days", days)
		} else {
			hours := int(duration.Hours())
			diag.ExpiresIn = fmt.Sprintf("%d hours", hours)
		}
	}

	// Build certificate info string
	var info strings.Builder
	info.WriteString(fmt.Sprintf("Subject: %s\n", cert.Subject.CommonName))
	info.WriteString(fmt.Sprintf("Issuer: %s\n", cert.Issuer.CommonName))
	info.WriteString(fmt.Sprintf("Valid From: %s\n", cert.NotBefore.Format("2006-01-02 15:04:05 MST")))
	info.WriteString(fmt.Sprintf("Valid Until: %s\n", cert.NotAfter.Format("2006-01-02 15:04:05 MST")))
	info.WriteString(fmt.Sprintf("Expires In: %s\n", diag.ExpiresIn))
	if len(cert.DNSNames) > 0 {
		info.WriteString(fmt.Sprintf("DNS Names: %s\n", strings.Join(cert.DNSNames, ", ")))
	}
	info.WriteString(fmt.Sprintf("Signature Algorithm: %s\n", cert.SignatureAlgorithm))
	info.WriteString(fmt.Sprintf("Public Key Algorithm: %s", cert.PublicKeyAlgorithm))

	diag.CertificateInfo = info.String()

	return diag
}

// FormatDNSDiagnostics returns a human-readable string of DNS diagnostics
func FormatDNSDiagnostics(diag *models.DNSDiagnostics) string {
	if diag.Error != "" {
		return fmt.Sprintf("DNS Error: %s", diag.Error)
	}

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("Hostname: %s\n", diag.Hostname))
	sb.WriteString(fmt.Sprintf("IP Addresses: %s\n", strings.Join(diag.IPAddresses, ", ")))
	sb.WriteString(fmt.Sprintf("Lookup Time: %s", diag.LookupTime))
	return sb.String()
}

// FormatSSLDiagnostics returns a human-readable string of SSL diagnostics
func FormatSSLDiagnostics(diag *models.SSLCertificateDiagnostics) string {
	if diag.Error != "" {
		return fmt.Sprintf("SSL Error: %s", diag.Error)
	}

	if !diag.Present {
		return "No SSL certificate (HTTP connection)"
	}

	var sb strings.Builder
	if diag.Valid {
		sb.WriteString("✓ Valid SSL Certificate\n\n")
	} else {
		sb.WriteString("✗ Invalid SSL Certificate\n\n")
	}

	sb.WriteString(diag.CertificateInfo)
	return sb.String()
}

// GetCertificateStatus returns a status string for UI display
func GetCertificateStatus(diag *models.SSLCertificateDiagnostics) string {
	if !diag.Present {
		return "none"
	}
	if diag.Valid {
		return "valid"
	}
	return "invalid"
}

// VerifyCertificateChain verifies the certificate chain
func VerifyCertificateChain(certs []*x509.Certificate, hostname string) error {
	if len(certs) == 0 {
		return fmt.Errorf("no certificates in chain")
	}

	// Create a certificate pool with the intermediate certificates
	intermediatePool := x509.NewCertPool()
	for i := 1; i < len(certs); i++ {
		intermediatePool.AddCert(certs[i])
	}

	// Verify the leaf certificate
	opts := x509.VerifyOptions{
		DNSName:       hostname,
		Intermediates: intermediatePool,
	}

	if _, err := certs[0].Verify(opts); err != nil {
		return fmt.Errorf("certificate verification failed: %v", err)
	}

	return nil
}

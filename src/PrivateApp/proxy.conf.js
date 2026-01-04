const ASPIRE_ENDPOINT = process.env['services__privateapi__https__0'] || process.env['services__privateapi__http__0'] || 'https://localhost:7038';

console.log('PrivateApp proxy configured for:', ASPIRE_ENDPOINT);

const config = {
  '/api/*': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'info',
    onError: (err, req, res) => {
      console.error('Proxy error:', err);
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log('Proxying request:', req.method, req.url, '→', ASPIRE_ENDPOINT + req.url);
    }
  },
  '/health': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'info'
  },
  '/alive': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'info'
  },
  '/hub/*': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    ws: true, // Enable WebSocket proxying for SignalR
    logLevel: 'info'
  }
};

module.exports = config;
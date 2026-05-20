/** @type {import('next').NextConfig} */

const nextConfig = {
  reactStrictMode: true,
  compress: true,
  poweredByHeader: false,
  webpack: (config, { dev }) => {
    if (dev) {
      // Avoid intermittent local .next cache corruption (missing vendor chunk modules).
      config.cache = false;
    }

    return config;
  },
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5156/api/:path*',
      },
    ];
  },
};

export default nextConfig;

# WordScape Blazor WASM - Static Web Deployment

This Blazor WebAssembly application can be deployed as static files to any web hosting service that supports static websites.

## Building for Production

To build and publish the application as static files:

```bash
dotnet publish -c Release -o publish
```

This creates a `publish/wwwroot` folder containing all the static files needed to run the application.

## Deployment Options

The static files in `publish/wwwroot` can be deployed to:

### Free Static Hosting Services
- **GitHub Pages** - Host directly from a GitHub repository
- **Netlify** - Drag and drop deployment or Git integration
- **Vercel** - Simple deployment with CLI or web interface
- **Firebase Hosting** - Google's static hosting service
- **Azure Static Web Apps** - Microsoft's static hosting with CI/CD

### Traditional Web Hosting
- Any web hosting provider that supports static files
- Upload the contents of `publish/wwwroot` to the web root directory

## Important Files

The `publish/wwwroot` directory contains:

- `index.html` - Main entry point
- `css/` - Stylesheets including the game styles
- `_framework/` - Blazor WASM runtime and your application DLLs
- `lib/` - Third-party libraries (Bootstrap)
- Game assets and resources

## MIME Type Requirements

Most modern web hosts automatically configure the correct MIME types. If you encounter issues, ensure these MIME types are configured:

- `.dll` files: `application/octet-stream`
- `.wasm` files: `application/wasm`
- `.json` files: `application/json`

## Base Path Configuration

If deploying to a subdirectory (e.g., `mysite.com/games/wordscape/`), update the `<base href="/" />` tag in `index.html` to match your deployment path:

```html
<base href="/games/wordscape/" />
```

## Testing Locally

To test the static files locally:

1. Install dotnet-serve tool:
   ```bash
   dotnet tool install --global dotnet-serve
   ```

2. Serve the files with SPA support:
   ```bash
   cd publish/wwwroot
   dotnet serve -p 8080 --fallback-file index.html
   ```

3. Open http://localhost:8080 in your browser

**Important:** The `--fallback-file index.html` option is crucial for Blazor WASM applications as it ensures all routes (like `/wordscape`) are handled by the main application instead of returning 404 errors.

## Performance Optimizations

The published files include:
- Compressed versions (.br and .gz) for faster loading
- Tree-shaken Blazor runtime (only includes used code)
- Minified CSS and JavaScript
- Optimized for static hosting with proper caching headers

## Single Page Application (SPA) Support

**Critical for Deployment:** Blazor WASM applications require SPA routing support to work correctly. Configure your hosting provider to serve `index.html` for all routes that don't correspond to static files.

### Common Hosting Configurations:

**Netlify**: Create a `_redirects` file in `publish/wwwroot/`:
```
/*    /index.html   200
```

**Vercel**: Create a `vercel.json` file in `publish/wwwroot/`:
```json
{
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

**Apache**: Add to `.htaccess` in `publish/wwwroot/`:
```apache
RewriteEngine On
RewriteCond %{REQUEST_FILENAME} !-f
RewriteCond %{REQUEST_FILENAME} !-d
RewriteRule . /index.html [L]
```

**Nginx**: Add to server configuration:
```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

This ensures direct navigation to routes like `/wordscape` works correctly.

## Example: Deploying to GitHub Pages

1. Create a new repository on GitHub
2. Upload the contents of `publish/wwwroot` to the repository
3. Enable GitHub Pages in repository settings
4. Your game will be available at `https://yourusername.github.io/repository-name`

The WordScape game runs entirely in the browser with no server dependencies!

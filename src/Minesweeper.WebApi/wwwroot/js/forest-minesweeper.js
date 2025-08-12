// Forest Minesweeper - JavaScript Game Logic
class ForestMinesweeper {
    constructor() {
        this.apiBase = '/api';
        this.token = localStorage.getItem('minesweeper_token');
        this.currentGame = null;
        this.gameTimer = null;
        this.gameStartTime = null;

        this.initializeEventListeners();
        this.initializeWelcomeButtons();
        this.checkAuthStatus();
        this.loadPlayerStats();
    }

    // Initialize event listeners
    initializeEventListeners() {
        // Auth buttons
        document.getElementById('loginBtn').addEventListener('click', () => this.showAuthModal('login'));
        document.getElementById('registerBtn').addEventListener('click', () => this.showAuthModal('register'));
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());
        document.getElementById('closeAuthModal').addEventListener('click', () => this.hideAuthModal());
        document.getElementById('authSwitchBtn').addEventListener('click', () => this.switchAuthMode());
        document.getElementById('authForm').addEventListener('submit', (e) => this.handleAuth(e));

        // Welcome action buttons
        document.getElementById('quickLoginBtn').addEventListener('click', () => this.showAuthModal('login'));
        document.getElementById('quickRegisterBtn').addEventListener('click', () => this.showAuthModal('register'));
        document.getElementById('quickPlayBtn').addEventListener('click', () => this.startNewGame());

        // Game controls
        document.getElementById('newGameBtn').addEventListener('click', () => this.startNewGame());
        document.getElementById('playAgainBtn').addEventListener('click', () => this.startNewGame());

        // Add new game control buttons
        document.getElementById('stopGameBtn').addEventListener('click', () => this.stopCurrentGame());
        document.getElementById('pauseGameBtn').addEventListener('click', () => this.togglePauseGame());

        // Close auth modal on overlay click
        document.getElementById('authOverlay').addEventListener('click', (e) => {
            if (e.target === document.getElementById('authOverlay')) {
                this.hideAuthModal();
            }
        });
    }

    // Helper function to convert numeric game status to string
    getGameStatus(status) {
        const statusMap = {
            0: 'NotStarted',
            1: 'InProgress',
            2: 'Won',
            3: 'Lost',
            4: 'Paused'
        };
        return statusMap[status] || status;
    }

    // Check if user is authenticated
    checkAuthStatus() {
        if (this.token) {
            this.showAuthenticatedState();
            this.loadActiveGames();
        } else {
            this.showUnauthenticatedState();
        }
    }

    // Show authenticated UI state
    showAuthenticatedState() {
        document.getElementById('authButtons').classList.add('hidden');
        document.getElementById('userInfo').classList.remove('hidden');

        // Update welcome actions
        document.getElementById('guestActions').classList.add('hidden');
        document.getElementById('authenticatedActions').classList.remove('hidden');

        // Decode token to get username (basic JWT decode)
        try {
            const payload = JSON.parse(atob(this.token.split('.')[1]));
            document.getElementById('username').textContent = payload.name || payload.username || 'Player';
        } catch (e) {
            console.error('Error decoding token:', e);
            this.logout();
        }
    }

    // Show unauthenticated UI state
    showUnauthenticatedState() {
        document.getElementById('authButtons').classList.remove('hidden');
        document.getElementById('userInfo').classList.add('hidden');

        // Update welcome actions
        document.getElementById('guestActions').classList.remove('hidden');
        document.getElementById('authenticatedActions').classList.add('hidden');
    }

    // Show authentication modal
    showAuthModal(mode = 'login') {
        const overlay = document.getElementById('authOverlay');
        const title = document.getElementById('authTitle');
        const emailGroup = document.getElementById('emailGroup');
        const passwordRequirements = document.getElementById('passwordRequirements');
        const submitBtn = document.getElementById('authSubmitBtn');
        const switchText = document.getElementById('authSwitchText');
        const switchBtn = document.getElementById('authSwitchBtn');

        if (mode === 'login') {
            title.textContent = 'Login to Forest Minesweeper';
            emailGroup.style.display = 'none';
            passwordRequirements.classList.add('hidden');
            submitBtn.innerHTML = '<i class="fas fa-sign-in-alt"></i> Login';
            switchText.textContent = "Don't have an account?";
            switchBtn.textContent = 'Register here';
            submitBtn.dataset.mode = 'login';
        } else {
            title.textContent = 'Join Forest Minesweeper';
            emailGroup.style.display = 'block';
            passwordRequirements.classList.remove('hidden');
            submitBtn.innerHTML = '<i class="fas fa-user-plus"></i> Register';
            switchText.textContent = "Already have an account?";
            switchBtn.textContent = 'Login here';
            submitBtn.dataset.mode = 'register';
        }

        overlay.classList.remove('hidden');
        document.getElementById('authUsername').focus();
    }

    // Hide authentication modal
    hideAuthModal() {
        document.getElementById('authOverlay').classList.add('hidden');
        document.getElementById('authForm').reset();
    }

    // Switch between login and register modes
    switchAuthMode() {
        const currentMode = document.getElementById('authSubmitBtn').dataset.mode;
        this.showAuthModal(currentMode === 'login' ? 'register' : 'login');
    }

    // Handle authentication form submission
    async handleAuth(e) {
        e.preventDefault();
        const mode = document.getElementById('authSubmitBtn').dataset.mode;
        const formData = new FormData(e.target);

        const data = {
            usernameOrEmail: formData.get('username'), // Backend expects usernameOrEmail
            password: formData.get('password')
        };

        if (mode === 'register') {
            data.username = formData.get('username'); // For registration, send both
            data.email = formData.get('email');
            delete data.usernameOrEmail; // Remove usernameOrEmail for registration
        }

        this.showLoading();

        try {
            const endpoint = mode === 'login' ? '/auth/login' : '/auth/register';
            console.log(`${mode} attempt:`, { endpoint, data, formFields: Object.fromEntries(formData) });

            const response = await this.apiCall(endpoint, 'POST', data, false);

            if (response.isSuccess) {
                console.log(`${mode} successful:`, response.data);
                this.token = response.data.token;
                localStorage.setItem('minesweeper_token', this.token);
                this.hideLoading();
                this.hideAuthModal();
                this.showAuthenticatedState();
                this.loadPlayerStats();
                this.loadActiveGames();
                this.showNotification(`Welcome${mode === 'register' ? ' to Forest Minesweeper' : ' back'}!`, 'success');
            } else {
                throw new Error(response.error || 'Authentication failed');
            }
        } catch (error) {
            this.hideLoading();
            this.showNotification(error.message, 'error');
        }
    }

    // Logout user
    logout() {
        this.token = null;
        localStorage.removeItem('minesweeper_token');
        this.currentGame = null;
        this.clearGameTimer();
        this.showUnauthenticatedState();
        this.clearGameBoard();
        this.clearActiveGames();
        this.showNotification('Logged out successfully', 'info');
    }

    // Stop current game
    stopCurrentGame() {
        if (this.currentGame) {
            this.currentGame = null;
            this.clearGameTimer();
            this.clearGameBoard();
            this.hideGameControls();
            this.showNotification('Game stopped', 'info');
        }
    }

    // Toggle pause/resume game
    togglePauseGame() {
        if (!this.currentGame) return;

        const pauseBtn = document.getElementById('pauseGameBtn');

        if (this.gameTimer) {
            // Pause the game
            this.clearGameTimer();
            pauseBtn.innerHTML = '<i class="fas fa-play"></i> Resume';
            pauseBtn.classList.add('paused');
            this.showNotification('Game paused', 'info');
        } else {
            // Resume the game
            this.startGameTimer();
            pauseBtn.innerHTML = '<i class="fas fa-pause"></i> Pause';
            pauseBtn.classList.remove('paused');
            this.showNotification('Game resumed', 'info');
        }
    }

    // Show game controls
    showGameControls() {
        document.getElementById('gameControls').classList.remove('hidden');
    }

    // Hide game controls
    hideGameControls() {
        document.getElementById('gameControls').classList.add('hidden');
        const pauseBtn = document.getElementById('pauseGameBtn');
        pauseBtn.innerHTML = '<i class="fas fa-pause"></i> Pause';
        pauseBtn.classList.remove('paused');
    }

    // Start a new game
    async startNewGame() {
        if (!this.token) {
            this.showAuthModal('login');
            return;
        }

        const difficulty = document.getElementById('difficultySelect').value;
        const difficultyNameMap = {
            'beginner': 'Beginner',
            'intermediate': 'Intermediate',
            'expert': 'Expert'
        };

        this.showLoading();

        try {
            const response = await this.apiCall('/games', 'POST', {
                difficultyName: difficultyNameMap[difficulty]
            });

            if (response.isSuccess) {
                this.currentGame = response.data;
                this.renderGameBoard();
                this.startGameTimer();
                this.updateGameInfo();
                this.hideGameStatusOverlay();
                this.showGameControls();
                this.loadActiveGames();
            } else {
                throw new Error(response.error || 'Failed to create game');
            }
        } catch (error) {
            this.showNotification(error.message, 'error');
        } finally {
            this.hideLoading();
        }
    }

    // Render the game board
    renderGameBoard() {
        if (!this.currentGame) {
            console.error('No current game to render');
            return;
        }

        console.log('Rendering game board for game:', this.currentGame.id);
        const board = this.currentGame.board;
        const gameBoard = document.getElementById('gameBoard');

        if (!gameBoard) {
            console.error('Game board element not found');
            return;
        }

        gameBoard.innerHTML = '';
        gameBoard.className = 'game-board';

        const grid = document.createElement('div');
        grid.className = 'mine-grid';
        grid.style.gridTemplateColumns = `repeat(${board.columns}, 30px)`;

        console.log(`Creating grid: ${board.columns}x${board.rows} with ${board.cells.length} rows`);

        for (let row = 0; row < board.rows; row++) {
            for (let col = 0; col < board.columns; col++) {
                const cell = document.createElement('div');
                cell.className = 'mine-cell';
                cell.dataset.row = row;
                cell.dataset.col = col;

                // Add event listeners
                cell.addEventListener('click', (e) => this.handleCellClick(e, row, col));
                cell.addEventListener('contextmenu', (e) => this.handleCellRightClick(e, row, col));

                grid.appendChild(cell);
            }
        }

        gameBoard.appendChild(grid);
        this.updateCellStates();

        // Ensure the game area is visible
        document.querySelector('.game-area').scrollIntoView({ behavior: 'smooth' });
    }

    // Update cell visual states
    updateCellStates() {
        if (!this.currentGame) return;

        const cells = document.querySelectorAll('.mine-cell');
        const board = this.currentGame.board;

        cells.forEach(cell => {
            const row = parseInt(cell.dataset.row);
            const col = parseInt(cell.dataset.col);
            const cellData = board.cells[row][col];

            cell.className = 'mine-cell';
            cell.textContent = '';

            if (cellData.isFlagged) {
                cell.classList.add('flagged');
                cell.innerHTML = '<i class="fas fa-flag"></i>';
            } else if (cellData.isRevealed) {
                cell.classList.add('revealed');
                if (cellData.hasMine) {
                    cell.classList.add('mine');
                    cell.innerHTML = '<i class="fas fa-bomb"></i>';
                } else if (cellData.adjacentMineCount > 0) {
                    cell.textContent = cellData.adjacentMineCount;
                    cell.dataset.count = cellData.adjacentMineCount;
                }
            }
        });
    }

    // Handle cell click (reveal)
    async handleCellClick(e, row, col) {
        e.preventDefault();
        const status = this.getGameStatus(this.currentGame?.status);
        if (!this.currentGame || (status !== 'InProgress' && status !== 'NotStarted')) return;

        console.log('🎯 Cell click debug:', {
            gameId: this.currentGame.id,
            gameIdType: typeof this.currentGame.id,
            row: row,
            col: col,
            gameStatus: status,
            rawGameStatus: this.currentGame.status
        });

        this.showLoading();

        try {
            const response = await this.apiCall(`/games/${this.currentGame.id}/reveal`, 'POST', {
                Row: row,
                Column: col
            });

            if (response.isSuccess) {
                this.currentGame = response.data;
                this.updateCellStates();
                this.updateGameInfo();
                this.checkGameEnd();
            } else {
                throw new Error(response.error || 'Failed to reveal cell');
            }
        } catch (error) {
            this.showNotification(error.message, 'error');
        } finally {
            this.hideLoading();
        }
    }

    // Handle cell right-click (flag)
    async handleCellRightClick(e, row, col) {
        e.preventDefault();
        const status = this.getGameStatus(this.currentGame?.status);
        if (!this.currentGame || (status !== 'InProgress' && status !== 'NotStarted')) return;

        try {
            const response = await this.apiCall(`/games/${this.currentGame.id}/flag`, 'POST', {
                Row: row,
                Column: col
            });

            if (response.isSuccess) {
                this.currentGame = response.data;
                this.updateCellStates();
                this.updateGameInfo();
            } else {
                throw new Error(response.error || 'Failed to flag cell');
            }
        } catch (error) {
            this.showNotification(error.message, 'error');
        }
    }

    // Update game information display
    updateGameInfo() {
        if (!this.currentGame) return;

        document.getElementById('minesLeft').textContent = this.currentGame.remainingMines || '--';
        document.getElementById('moveCount').textContent = this.currentGame.moveCount || '0';
    }

    // Check if game has ended
    checkGameEnd() {
        if (!this.currentGame) return;

        const status = this.getGameStatus(this.currentGame.status);
        if (status === 'Won') {
            this.clearGameTimer();
            this.hideGameControls();
            this.showGameStatusOverlay('🎉', 'Congratulations!', 'You cleared the forest safely!', 'success');
            this.loadPlayerStats();
        } else if (status === 'Lost') {
            this.clearGameTimer();
            this.hideGameControls();
            this.showGameStatusOverlay('💥', 'Game Over', 'You hit a mine! Try again.', 'error');
            this.loadPlayerStats();
        }
    }

    // Show game status overlay
    showGameStatusOverlay(icon, title, message, type) {
        const overlay = document.getElementById('gameStatusOverlay');
        const statusIcon = document.getElementById('statusIcon');
        const statusTitle = document.getElementById('statusTitle');
        const statusMessage = document.getElementById('statusMessage');

        statusIcon.textContent = icon;
        statusTitle.textContent = title;
        statusMessage.textContent = message;

        overlay.classList.remove('hidden');
    }

    // Hide game status overlay
    hideGameStatusOverlay() {
        document.getElementById('gameStatusOverlay').classList.add('hidden');
    }

    // Start game timer
    startGameTimer() {
        this.gameStartTime = Date.now();
        this.gameTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - this.gameStartTime) / 1000);
            const minutes = Math.floor(elapsed / 60).toString().padStart(2, '0');
            const seconds = (elapsed % 60).toString().padStart(2, '0');
            document.getElementById('gameTimer').textContent = `${minutes}:${seconds}`;
        }, 1000);
    }

    // Clear game timer
    clearGameTimer() {
        if (this.gameTimer) {
            clearInterval(this.gameTimer);
            this.gameTimer = null;
        }
    }

    // Clear game board
    clearGameBoard() {
        const gameBoard = document.getElementById('gameBoard');
        gameBoard.innerHTML = `
            <div class="welcome-message">
                <div class="welcome-content">
                    <i class="fas fa-leaf welcome-icon"></i>
                    <h2>Welcome to Forest Minesweeper!</h2>
                    <p>Experience the enchanted forest adventure with modern minesweeper gameplay.</p>
                    
                    <!-- Call-to-Action Buttons -->
                    <div class="welcome-actions" id="welcomeActions">
                        <div class="action-buttons guest-actions" id="guestActions">
                            <button class="btn btn-primary btn-large" id="quickLoginBtn">
                                <i class="fas fa-sign-in-alt"></i> Log In to Play
                            </button>
                            <button class="btn btn-secondary btn-large" id="quickRegisterBtn">
                                <i class="fas fa-user-plus"></i> Register & Start
                            </button>
                        </div>
                        <div class="action-buttons authenticated-actions hidden" id="authenticatedActions">
                            <button class="btn btn-primary btn-large" id="quickPlayBtn">
                                <i class="fas fa-play"></i> Start Playing Now!
                            </button>
                            <p class="play-description">Choose your difficulty and begin your forest adventure</p>
                        </div>
                    </div>
                    
                    <div class="forest-elements">
                        <i class="fas fa-leaf maple-leaf-anim"></i>
                        <i class="fas fa-seedling acorn-anim"></i>
                        <i class="fas fa-tree"></i>
                    </div>
                </div>
            </div>
        `;

        // Re-initialize event listeners for the new buttons
        this.initializeWelcomeButtons();

        // Update the auth state for the welcome actions
        if (this.token) {
            document.getElementById('guestActions').classList.add('hidden');
            document.getElementById('authenticatedActions').classList.remove('hidden');
        } else {
            document.getElementById('guestActions').classList.remove('hidden');
            document.getElementById('authenticatedActions').classList.add('hidden');
        }

        // Hide game controls
        this.hideGameControls();

        // Reset game info
        document.getElementById('minesLeft').textContent = '--';
        document.getElementById('gameTimer').textContent = '00:00';
        document.getElementById('moveCount').textContent = '0';
    }

    // Initialize welcome button event listeners
    initializeWelcomeButtons() {
        const quickLoginBtn = document.getElementById('quickLoginBtn');
        const quickRegisterBtn = document.getElementById('quickRegisterBtn');
        const quickPlayBtn = document.getElementById('quickPlayBtn');

        if (quickLoginBtn) {
            quickLoginBtn.addEventListener('click', () => this.showAuthModal('login'));
        }
        if (quickRegisterBtn) {
            quickRegisterBtn.addEventListener('click', () => this.showAuthModal('register'));
        }
        if (quickPlayBtn) {
            quickPlayBtn.addEventListener('click', () => this.startNewGame());
        }
    }

    // Load player statistics
    async loadPlayerStats() {
        if (!this.token) return;

        try {
            // For now, show placeholder stats
            // TODO: Implement player statistics API endpoints
            document.getElementById('gamesPlayed').textContent = '0';
            document.getElementById('gamesWon').textContent = '0';
            document.getElementById('winRate').textContent = '0%';
            document.getElementById('bestTime').textContent = '--:--';
        } catch (error) {
            console.error('Failed to load player stats:', error);
        }
    }

    // Load active games
    async loadActiveGames() {
        if (!this.token) return;

        try {
            const response = await this.apiCall('/games/active', 'GET');

            if (response.isSuccess) {
                this.renderActiveGames(response.data);
            }
        } catch (error) {
            console.error('Failed to load active games:', error);
        }
    }

    // Render active games list
    renderActiveGames(games) {
        const gamesList = document.getElementById('activeGamesList');

        if (!games || games.length === 0) {
            gamesList.innerHTML = `
                <div class="no-games">
                    <i class="fas fa-leaf"></i>
                    <p>No active games</p>
                </div>
            `;
            return;
        }

        gamesList.innerHTML = games.map(game => `
            <div class="game-item" data-game-id="${game.id}">
                <div class="game-info">
                    <span class="difficulty">${game.difficulty.name}</span>
                    <span class="status">${game.status}</span>
                </div>
                <button class="btn btn-secondary btn-sm" onclick="window.minesweeper.loadGame('${game.id}')">
                    Load
                </button>
            </div>
        `).join('');
    }

    // Clear active games list
    clearActiveGames() {
        document.getElementById('activeGamesList').innerHTML = `
            <div class="no-games">
                <i class="fas fa-leaf"></i>
                <p>No active games</p>
            </div>
        `;
    }

    // Load a specific game
    async loadGame(gameId) {
        if (!this.token) return;

        this.showLoading();

        try {
            const response = await this.apiCall(`/games/${gameId}`, 'GET');

            if (response.isSuccess) {
                this.currentGame = response.data;
                this.renderGameBoard();
                this.updateGameInfo();

                const status = this.getGameStatus(this.currentGame.status);
                if (status === 'InProgress') {
                    this.startGameTimer();
                    this.showGameControls();
                } else {
                    this.hideGameControls();
                    this.checkGameEnd();
                }
            } else {
                throw new Error(response.error || 'Failed to load game');
            }
        } catch (error) {
            this.showNotification(error.message, 'error');
        } finally {
            this.hideLoading();
        }
    }

    // API call helper
    async apiCall(endpoint, method = 'GET', data = null, includeAuth = true) {
        const url = `${this.apiBase}${endpoint}`;
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            }
        };

        if (includeAuth && this.token) {
            options.headers['Authorization'] = `Bearer ${this.token}`;
        }

        if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
            options.body = JSON.stringify(data);
        }

        console.log(`🚀 API Call Details:`, {
            fullUrl: url,
            method: method,
            endpoint: endpoint,
            apiBase: this.apiBase,
            data: data,
            headers: options.headers,
            body: options.body
        });

        try {
            const response = await fetch(url, options);
            console.log(`📡 API Response:`, {
                status: response.status,
                statusText: response.statusText,
                url: response.url,
                headers: Object.fromEntries(response.headers.entries())
            });

            if (response.status === 401) {
                this.logout();
                throw new Error('Authentication required');
            }

            // Check if response has content before trying to parse JSON
            const contentType = response.headers.get('content-type');
            let result = null;

            if (contentType && contentType.includes('application/json')) {
                const text = await response.text();
                console.log('📄 Response text:', text);
                if (text.trim()) {
                    try {
                        result = JSON.parse(text);
                        console.log('✅ Parsed JSON:', result);
                    } catch (e) {
                        console.error('❌ Failed to parse JSON:', text, e);
                        throw new Error('Invalid JSON response from server');
                    }
                }
            }

            if (!response.ok) {
                const errorMessage = result?.error || result?.message || `HTTP ${response.status}: ${response.statusText}`;
                console.error('❌ API Error Details:', {
                    status: response.status,
                    statusText: response.statusText,
                    url,
                    method,
                    data,
                    result,
                    errorMessage
                });
                throw new Error(errorMessage);
            }

            return {
                isSuccess: true,
                data: result
            };
        } catch (error) {
            console.error('🔥 API Call Exception:', error);
            throw error;
        }
    }

    // Show loading overlay
    showLoading() {
        document.getElementById('loadingOverlay').classList.remove('hidden');
    }

    // Hide loading overlay
    hideLoading() {
        document.getElementById('loadingOverlay').classList.add('hidden');
    }

    // Show notification
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
                <span>${message}</span>
            </div>
        `;

        // Add styles if not already added
        if (!document.querySelector('#notification-styles')) {
            const style = document.createElement('style');
            style.id = 'notification-styles';
            style.textContent = `
                .notification {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    padding: 1rem 1.5rem;
                    border-radius: 0.5rem;
                    color: white;
                    font-weight: 500;
                    z-index: 10000;
                    animation: slideIn 0.3s ease;
                    max-width: 400px;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.2);
                }
                .notification-success { background: linear-gradient(135deg, #22c55e, #16a34a); }
                .notification-error { background: linear-gradient(135deg, #ef4444, #dc2626); }
                .notification-info { background: linear-gradient(135deg, #3b82f6, #2563eb); }
                .notification-content { display: flex; align-items: center; gap: 0.5rem; }
                @keyframes slideIn {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
                @keyframes slideOut {
                    from { transform: translateX(0); opacity: 1; }
                    to { transform: translateX(100%); opacity: 0; }
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 5000);
    }
}

// Initialize the game when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.minesweeper = new ForestMinesweeper();
});

// Handle page visibility change to pause/resume timer
document.addEventListener('visibilitychange', () => {
    if (window.minesweeper) {
        if (document.hidden && window.minesweeper.gameTimer) {
            // Pause timer when tab is hidden
            window.minesweeper.clearGameTimer();
        } else if (!document.hidden && window.minesweeper.currentGame) {
            const status = window.minesweeper.getGameStatus(window.minesweeper.currentGame.status);
            if (status === 'InProgress') {
                // Resume timer when tab becomes visible
                window.minesweeper.startGameTimer();
            }
        }
    }
});

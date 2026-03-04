-- ═══════════════════════════════════════════════════════════════
-- PLANEROO - Database Schema
-- Intelligent Educational Planning Platform
-- ═══════════════════════════════════════════════════════════════

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ── Parents ──
CREATE TABLE IF NOT EXISTS parents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(256) NOT NULL UNIQUE,
    password_hash VARCHAR(512) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    avatar_url VARCHAR(500),
    phone_number VARCHAR(20),
    timezone VARCHAR(50) DEFAULT 'Europe/Paris',
    language VARCHAR(10) DEFAULT 'fr',
    is_email_verified BOOLEAN DEFAULT FALSE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    refresh_token VARCHAR(512),
    refresh_token_expiry_time TIMESTAMP WITH TIME ZONE,
    has_accepted_terms BOOLEAN DEFAULT FALSE,
    terms_accepted_at TIMESTAMP WITH TIME ZONE,
    data_processing_consent BOOLEAN DEFAULT FALSE,
    notify_by_email BOOLEAN DEFAULT TRUE,
    notify_by_push BOOLEAN DEFAULT TRUE,
    weekly_report_enabled BOOLEAN DEFAULT TRUE,
    parent_lock_pin VARCHAR(10),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- ── Children ──
CREATE TABLE IF NOT EXISTS children (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id UUID NOT NULL REFERENCES parents(id) ON DELETE CASCADE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100),
    date_of_birth DATE NOT NULL,
    grade_level INTEGER NOT NULL CHECK (grade_level BETWEEN 1 AND 12),
    avatar_url VARCHAR(500),
    school_name VARCHAR(200),
    pin VARCHAR(10),
    total_xp INTEGER DEFAULT 0,
    current_level INTEGER DEFAULT 1,
    current_streak INTEGER DEFAULT 0,
    longest_streak INTEGER DEFAULT 0,
    last_activity_date TIMESTAMP WITH TIME ZONE,
    favorite_color VARCHAR(20),
    mascot_name VARCHAR(50) DEFAULT 'Roo',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_children_parent_id ON children(parent_id);

-- ── Scan Sessions ──
CREATE TABLE IF NOT EXISTS scan_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500),
    raw_ocr_text TEXT,
    processed_text TEXT,
    status VARCHAR(30) DEFAULT 'Processing',
    detected_tasks_count INTEGER DEFAULT 0,
    confirmed_tasks_count INTEGER DEFAULT 0,
    confidence_score DOUBLE PRECISION,
    error_message VARCHAR(1000),
    processed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- ── Homeworks ──
CREATE TABLE IF NOT EXISTS homeworks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    scan_session_id UUID REFERENCES scan_sessions(id) ON DELETE SET NULL,
    title VARCHAR(300) NOT NULL,
    description VARCHAR(2000),
    subject VARCHAR(50) NOT NULL,
    status VARCHAR(30) DEFAULT 'Pending',
    priority VARCHAR(30) DEFAULT 'Medium',
    due_date TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    estimated_minutes INTEGER DEFAULT 30,
    actual_minutes INTEGER,
    xp_reward INTEGER DEFAULT 10,
    notes VARCHAR(1000),
    is_auto_detected BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_homeworks_child_due ON homeworks(child_id, due_date);
CREATE INDEX idx_homeworks_child_status ON homeworks(child_id, status);

-- ── Planning Slots ──
CREATE TABLE IF NOT EXISTS planning_slots (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    homework_id UUID REFERENCES homeworks(id) ON DELETE SET NULL,
    day_of_week VARCHAR(20) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    slot_type VARCHAR(20) DEFAULT 'Study',
    title VARCHAR(200),
    notes VARCHAR(500),
    is_completed BOOLEAN DEFAULT FALSE,
    is_auto_generated BOOLEAN DEFAULT FALSE,
    week_number INTEGER NOT NULL,
    year INTEGER NOT NULL,
    "order" INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_planning_child_week ON planning_slots(child_id, week_number, year);

-- ── Badges ──
CREATE TABLE IF NOT EXISTS badges (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(300) NOT NULL,
    icon_name VARCHAR(100) NOT NULL,
    category VARCHAR(30) NOT NULL,
    xp_reward INTEGER DEFAULT 50,
    earned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_new BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_badges_child ON badges(child_id);

-- ── Notifications ──
CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id UUID NOT NULL REFERENCES parents(id) ON DELETE CASCADE,
    child_id UUID,
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    type VARCHAR(30) NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    action_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_notifications_parent_read ON notifications(parent_id, is_read);

-- ── AI Interactions ──
CREATE TABLE IF NOT EXISTS ai_interactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    user_message VARCHAR(2000) NOT NULL,
    ai_response VARCHAR(5000) NOT NULL,
    topic VARCHAR(100),
    was_filtered BOOLEAN DEFAULT FALSE,
    filter_reason VARCHAR(500),
    parent_reviewed BOOLEAN DEFAULT FALSE,
    safety_score DOUBLE PRECISION,
    tokens_used INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_ai_interactions_child ON ai_interactions(child_id);

-- ── Study Sheets ──
CREATE TABLE IF NOT EXISTS study_sheets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    child_id UUID NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    homework_id UUID,
    title VARCHAR(300) NOT NULL,
    subject VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    summary VARCHAR(1000),
    target_age INTEGER NOT NULL,
    grade_level INTEGER NOT NULL,
    is_favorite BOOLEAN DEFAULT FALSE,
    view_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_study_sheets_child ON study_sheets(child_id);

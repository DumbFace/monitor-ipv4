# Monitor IPv4

A distributed system designed to monitor dynamic public IP changes and ensure reliable remote access (SSH/VPN) by automatically updating and notifying when IP changes occur.

## Problem

Public IP addresses from ISPs frequently change, causing issues with:

- SSH access to home servers
- VPN connectivity
- Port forwarding setups

Manual tracking is unreliable and inconvenient.

## Solution

This system automatically:

- Detects public IP changes
- Sends email notifications
- Updates IP in Firebase for real-time access
- Publishes events for further processing

## Architecture

The system consists of 3 main components:

### 1. monitor-ipv4-client

- Runs on local machine
- Detects network/VPN changes
- Ensures correct IP is tracked for remote access

### 2. monitor-ipv4-tool

- Core monitoring service
- Detects IP changes
- Stores data in SQLite
- Sends email notifications
- Publishes events to RabbitMQ

### 3. firebase-worker

- Consumes messages from RabbitMQ
- Updates Firebase Realtime Database
- Provides real-time IP access

-- =========================================================================
-- VET-MS  ·  COMPREHENSIVE DEMO DATA
-- Run this script AFTER the application has performed its first launch
-- (which creates tables and seeds the admin user + base lookup data).
--
-- What this adds:
--   • 2 Vet staff users  (Dr. Sarah Chen, Dr. Marcus Webb)
--   • 4 additional Breeds
--   • 4 additional Customers
--   • 5 additional Pets  (diverse species, ages, conditions)
--   • 40+ Appointments   (varied statuses, services, vets)
--   • 18  Medical Records (realistic diagnoses & treatments)
--   • 14  CBC Records     (some with flagged abnormal values)
--   • Medication records  linked to visits
-- =========================================================================

-- ── Guard: skip if demo data was already loaded ───────────────────────────
DO $$ BEGIN
  IF (SELECT COUNT(*) FROM users WHERE username IN ('dr.chen', 'dr.webb')) > 0 THEN
    RAISE NOTICE 'Demo data already loaded — skipping.';
    RETURN;
  END IF;
END $$;

-- =========================================================================
-- 1.  VET STAFF USERS
-- =========================================================================
INSERT INTO users (username, password_hash, full_name, email, role, is_active, created_by) VALUES
('dr.chen', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
 'Dr. Sarah Chen', 'sarah.chen@vetms.local', 'Veterinarian', true, 'System'),
('dr.webb', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
 'Dr. Marcus Webb', 'marcus.webb@vetms.local', 'Veterinarian', true, 'System');
-- Both vet accounts use password: vet123

-- =========================================================================
-- 2.  ADDITIONAL BREEDS
-- =========================================================================
INSERT INTO breeds (species_id, name, description, is_active, created_by) VALUES
(1, 'Labrador Retriever', 'Gentle, intelligent and family-friendly sporting dog.', true, 'System'),
(1, 'Beagle',             'Compact scent hound, curious and energetic.', true, 'System'),
(2, 'Maine Coon',         'Large, sociable cat with tufted ears and a bushy tail.', true, 'System'),
(4, 'Holland Lop',        'Compact lop-eared rabbit, docile and sociable.', true, 'System');

-- =========================================================================
-- 3.  ADDITIONAL SERVICE TYPES
-- =========================================================================
INSERT INTO service_types (name, category, price, description, is_active, created_by) VALUES
('Spay / Neuter',             'Surgery',      250.00, 'Routine surgical sterilization procedure.',              true, 'System'),
('Orthopedic Consultation',   'Consultation', 95.00,  'Specialist review for bone/joint conditions.',           true, 'System'),
('Allergy Test Panel',        'Diagnostic',   180.00, 'Intradermal or serum-based allergy panel.',              true, 'System'),
('Renal Function Panel',      'Diagnostic',   110.00, 'BUN, creatinine, phosphorus, urine SG.',                 true, 'System'),
('Ultrasound (Abdominal)',    'Diagnostic',   160.00, 'Real-time soft-tissue abdominal imaging.',               true, 'System'),
('Wellness / Puppy Exam',     'Consultation', 45.00,  'New-patient or puppy/kitten wellness check.',            true, 'System'),
('Microchipping',             'Identification', 35.00,'ISO-standard chip implant + registry.',                  true, 'System'),
('Euthanasia (Compassionate)','End of Life',  150.00, 'Compassionate end-of-life service.',                     false,'System');

-- =========================================================================
-- 4.  ADDITIONAL MEDICATIONS
-- =========================================================================
INSERT INTO medications (name, category, dosage_form, unit, description, is_active, created_by) VALUES
('Apoquel (Oclacitinib)',    'Dermatological',  'Tablet',     '16mg',      'Rapid itch relief for allergic dermatitis.',             true, 'System'),
('Cerenia (Maropitant)',     'Antiemetic',      'Injectable', '10mg/ml',   'Nausea / vomiting control, pre-op anti-emetic.',         true, 'System'),
('Atenolol',                 'Cardiac',         'Tablet',     '25mg',      'Beta-blocker for feline hypertrophic cardiomyopathy.',    true, 'System'),
('Baytril (Enrofloxacin)',   'Antibiotic',      'Tablet',     '50mg',      'Fluoroquinolone for respiratory & urinary infections.',   true, 'System'),
('Prednisolone',             'Corticosteroid',  'Tablet',     '5mg',       'Immune suppression and anti-inflammatory.',               true, 'System'),
('Tramadol',                 'Analgesic',       'Tablet',     '50mg',      'Opioid analgesic for moderate-to-severe pain.',           true, 'System'),
('Gabapentin',               'Neurological',    'Capsule',    '100mg',     'Neuropathic pain management and seizure control.',        true, 'System'),
('Benazepril',               'ACE Inhibitor',   'Tablet',     '5mg',       'Slows CKD progression; reduces proteinuria in cats.',     true, 'System'),
('Phosphorus Binder (Lante)', 'Renal Support',  'Chewable',  '250mg',     'Binds intestinal phosphorus; adjunct for CKD cats.',      true, 'System'),
('Doxycycline',              'Antibiotic',      'Tablet',     '100mg',     'Broad-spectrum for tick-borne & respiratory disease.',    true, 'System');

-- =========================================================================
-- 5.  ADDITIONAL CUSTOMERS
-- =========================================================================
INSERT INTO customers (full_name, phone, email, address, notes, is_active, created_by) VALUES
('Robert Taylor',   '555-2200', 'robert.taylor@email.com',  '78 Elm Street, Apt 3',        'Owns two dogs — Max and Biscuit (not yet registered).', true, 'System'),
('Maria Santos',    '555-3311', 'maria.santos@email.com',   '22 Blossom Lane',              'Meticulous owner; keeps detailed home health notes.',   true, 'System'),
('Kevin Park',      '555-4422', 'kevin.park@email.com',     '9 Riverside Drive, Suite 1B', 'First-time pet owner; needs extra coaching.',           true, 'System'),
('Linda Nguyen',    '555-5533', 'linda.nguyen@email.com',   '304 Cedar Road',               'Breeder; has a colony of Holland Lops.',                true, 'System');

-- =========================================================================
-- 6.  ADDITIONAL PETS
-- =========================================================================
-- We resolve customer IDs via subqueries so the script is position-safe.
INSERT INTO pets
  (customer_id, customer_name, species_id, species_name, breed_id, breed_name,
   name, gender, date_of_birth, weight, color, microchip_no, notes, is_active, created_by)
VALUES
(
  (SELECT id FROM customers WHERE full_name = 'Robert Taylor'),
  'Robert Taylor', 1, 'Dog',
  (SELECT id FROM breeds WHERE name = 'German Shepherd'),
  'German Shepherd',
  'Max', 'Male', '2020-03-10', 34.5, 'Black & Tan', 'MC445566',
  'Max was diagnosed with bilateral hip dysplasia at age 2 (PennHIP score 0.72). ' ||
  'He is on a joint-support diet and monthly hydrotherapy. Exercise is strictly controlled. ' ||
  'Owner is very engaged and performs daily range-of-motion stretches as instructed. ' ||
  'Temperament: confident, slightly reactive on leash toward other dogs. ' ||
  'Allergic to chicken protein — switched to salmon-based kibble with good results. ' ||
  'Currently maintained on Meloxicam on high-activity days only.',
  true, 'System'
),
(
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'),
  'Maria Santos', 2, 'Cat',
  (SELECT id FROM breeds WHERE name = 'Siamese Cat'),
  'Siamese Cat',
  'Mochi', 'Female', '2021-11-05', 3.8, 'Seal Point', 'MC778899',
  'Mochi was diagnosed with Stage 2 Chronic Kidney Disease at 24 months. ' ||
  'She is on a renal-formula diet (Hills k/d), Benazepril 2.5mg daily, and phosphorus binder. ' ||
  'Quarterly renal panels and bi-annual CBC are mandatory. GFR is currently stable. ' ||
  'Owner is extremely compliant with medication schedule. ' ||
  'Mochi is a shy cat and requires patience during examination. Use low-stress handling protocols.',
  true, 'System'
),
(
  (SELECT id FROM customers WHERE full_name = 'Kevin Park'),
  'Kevin Park', 1, 'Dog',
  (SELECT id FROM breeds WHERE name = 'Labrador Retriever'),
  'Labrador Retriever',
  'Cooper', 'Male', '2025-09-20', 12.8, 'Yellow', 'MC990011',
  'Cooper is a 7-month-old Labrador puppy, currently completing his puppy vaccination series. ' ||
  'Excellent temperament — social with people and other dogs. ' ||
  'Owner is a first-time dog owner and has been counselled on puppy nutrition, crate training, and socialization windows. ' ||
  'Scheduled for neuter at 12 months.',
  true, 'System'
),
(
  (SELECT id FROM customers WHERE full_name = 'Linda Nguyen'),
  'Linda Nguyen', 4, 'Rabbit',
  (SELECT id FROM breeds WHERE name = 'Holland Lop'),
  'Holland Lop',
  'Pebble', 'Female', '2024-06-14', 1.9, 'Grey & White', '',
  'Pebble is one of Linda''s breeding does. ' ||
  'Presented for GI stasis episode in January 2025 — fully recovered. ' ||
  'Diet is hay-based with minimal pellets. Owner checks gut motility daily.',
  true, 'System'
),
(
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'),
  'Maria Santos', 4, 'Rabbit',
  (SELECT id FROM breeds WHERE name = 'Holland Lop'),
  'Holland Lop',
  'Coco', 'Male', '2025-07-01', 1.4, 'Chocolate Brown', '',
  'Coco is a young Holland Lop, recently adopted. ' ||
  'First wellness exam completed. Healthy baseline; scheduled for neuter at 5 months.',
  true, 'System'
);

-- =========================================================================
-- 7.  APPOINTMENTS
--     Covers: Max, Mochi, Cooper, Pebble, Coco  (Buddy + Luna already seeded)
-- =========================================================================
-- Helpers: vet IDs
-- admin (System Administrator)  = (SELECT id FROM users WHERE username = 'admin')
-- dr.chen                        = (SELECT id FROM users WHERE username = 'dr.chen')
-- dr.webb                        = (SELECT id FROM users WHERE username = 'dr.webb')

-- ── MAX (German Shepherd) ──────────────────────────────────────────────────
INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP - INTERVAL '730 days', 30, 'Completed',
  'First annual exam after adoption.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'X-Ray'), 'X-Ray',
  CURRENT_TIMESTAMP - INTERVAL '680 days', 45, 'Completed',
  'Bilateral hip radiographs for PennHIP scoring.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'Orthopedic Consultation'), 'Orthopedic Consultation',
  CURRENT_TIMESTAMP - INTERVAL '620 days', 60, 'Completed',
  'Hip dysplasia management plan: weight control, physio, Meloxicam as needed.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Vaccination'), 'Vaccination',
  CURRENT_TIMESTAMP - INTERVAL '365 days', 20, 'Completed',
  'Annual DHPPL booster + bordetella.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP - INTERVAL '180 days', 30, 'Completed',
  '6-month hip re-evaluation; weight down 1.5 kg — good progress.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'Allergy Test Panel'), 'Allergy Test Panel',
  CURRENT_TIMESTAMP - INTERVAL '60 days', 45, 'Completed',
  'Suspected chicken protein allergy confirmed. Switching diet.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'Orthopedic Consultation'), 'Orthopedic Consultation',
  CURRENT_TIMESTAMP + INTERVAL '30 days', 60, 'Scheduled',
  'Annual orthopedic review — hip progression assessment.', 'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

-- ── MOCHI (CKD Siamese Cat) ────────────────────────────────────────────────
INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP - INTERVAL '540 days', 40, 'Completed',
  'Increased thirst and mild weight loss noted at home.', 'System'
FROM pets p WHERE p.name = 'Mochi';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Renal Function Panel'), 'Renal Function Panel',
  CURRENT_TIMESTAMP - INTERVAL '535 days', 30, 'Completed',
  'BUN elevated at 52 mg/dL, creatinine 2.8 mg/dL. Stage 2 CKD confirmed.', 'System'
FROM pets p WHERE p.name = 'Mochi';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Renal Function Panel'), 'Renal Function Panel',
  CURRENT_TIMESTAMP - INTERVAL '270 days', 30, 'Completed',
  'Quarterly monitoring. Creatinine stable at 2.6 — diet change helping.', 'System'
FROM pets p WHERE p.name = 'Mochi';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Renal Function Panel'), 'Renal Function Panel',
  CURRENT_TIMESTAMP - INTERVAL '90 days', 30, 'Completed',
  'Creatinine 2.5 — slight improvement. Continue current protocol.', 'System'
FROM pets p WHERE p.name = 'Mochi';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Renal Function Panel'), 'Renal Function Panel',
  CURRENT_TIMESTAMP + INTERVAL '90 days', 30, 'Scheduled',
  'Next quarterly renal panel.', 'System'
FROM pets p WHERE p.name = 'Mochi';

-- ── COOPER (Labrador Puppy) ────────────────────────────────────────────────
INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Wellness / Puppy Exam'), 'Wellness / Puppy Exam',
  CURRENT_TIMESTAMP - INTERVAL '120 days', 40, 'Completed',
  'First puppy exam at 8 weeks. Healthy baseline. No abnormalities detected.', 'System'
FROM pets p WHERE p.name = 'Cooper';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Vaccination'), 'Vaccination',
  CURRENT_TIMESTAMP - INTERVAL '100 days', 20, 'Completed',
  'DA2PP first dose (8-week series).', 'System'
FROM pets p WHERE p.name = 'Cooper';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Vaccination'), 'Vaccination',
  CURRENT_TIMESTAMP - INTERVAL '79 days', 20, 'Completed',
  'DA2PP second dose (12-week booster).', 'System'
FROM pets p WHERE p.name = 'Cooper';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Vaccination'), 'Vaccination',
  CURRENT_TIMESTAMP - INTERVAL '58 days', 20, 'Completed',
  'DA2PP third dose + rabies (16-week final puppy series complete).', 'System'
FROM pets p WHERE p.name = 'Cooper';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Microchipping'), 'Microchipping',
  CURRENT_TIMESTAMP - INTERVAL '58 days', 10, 'Completed',
  'ISO chip implanted at same visit as 16-week vaccines.', 'System'
FROM pets p WHERE p.name = 'Cooper';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Spay / Neuter'), 'Spay / Neuter',
  CURRENT_TIMESTAMP + INTERVAL '60 days', 120, 'Scheduled',
  'Scheduled neuter at approx. 12 months of age.', 'System'
FROM pets p WHERE p.name = 'Cooper';

-- ── PEBBLE (Holland Lop Rabbit) ────────────────────────────────────────────
INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Pebble', p.customer_id, 'Linda Nguyen',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP - INTERVAL '200 days', 30, 'Completed',
  'Emergency GI stasis presentation. Gut motility severely reduced.', 'System'
FROM pets p WHERE p.name = 'Pebble';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Pebble', p.customer_id, 'Linda Nguyen',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP - INTERVAL '190 days', 20, 'Completed',
  'Follow-up post-GI stasis — gut sounds normal, eating well.', 'System'
FROM pets p WHERE p.name = 'Pebble';

INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Pebble', p.customer_id, 'Linda Nguyen',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  (SELECT id FROM service_types WHERE name = 'General Checkup'), 'General Checkup',
  CURRENT_TIMESTAMP + INTERVAL '160 days', 30, 'Scheduled',
  'Annual wellness exam.', 'System'
FROM pets p WHERE p.name = 'Pebble';

-- ── COCO (Holland Lop Rabbit) ──────────────────────────────────────────────
INSERT INTO appointments
  (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name,
   service_type_id, service_type_name, appointment_date, duration, status, notes, created_by)
SELECT
  p.id, 'Coco', p.customer_id, 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  (SELECT id FROM service_types WHERE name = 'Wellness / Puppy Exam'), 'Wellness / Puppy Exam',
  CURRENT_TIMESTAMP - INTERVAL '14 days', 30, 'Completed',
  'New patient wellness exam. Healthy 3-month-old Holland Lop.', 'System'
FROM pets p WHERE p.name = 'Coco';

-- =========================================================================
-- 8.  MEDICAL RECORDS
-- =========================================================================

-- ── MAX ────────────────────────────────────────────────────────────────────
INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor')),
  'Max',
  (SELECT id FROM customers WHERE full_name = 'Robert Taylor'), 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  'Healthy adult German Shepherd. Slight muscle tension in lumbar region, likely exercise-related.',
  'Rest for 5 days. Warm compress to lumbar area twice daily.',
  'No neurological deficits detected. Weight 36.0 kg — slightly above target. Discussed caloric restriction.',
  NULL, 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND a.service_type_name = 'General Checkup'
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '700 days'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor')),
  'Max',
  (SELECT id FROM customers WHERE full_name = 'Robert Taylor'), 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  'Bilateral hip dysplasia confirmed — PennHIP distraction index 0.72 (L) / 0.68 (R). Grade 2 osteoarthritis changes.',
  'Orthopedic management plan: restrict high-impact activity, monthly hydrotherapy, Meloxicam 0.1 mg/kg on symptomatic days, joint-support diet (Hills j/d).',
  'PennHIP score significantly above breed median. Long-term prognosis for function is good with consistent management. Owner counselled on signs of pain progression.',
  CURRENT_TIMESTAMP - INTERVAL '640 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND a.service_type_name = 'X-Ray'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor')),
  'Max',
  (SELECT id FROM customers WHERE full_name = 'Robert Taylor'), 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'Weight improved to 34.5 kg (-1.5 kg). Hip ROM unchanged. No acute pain response on palpation.',
  'Continue Meloxicam as-needed. Physio to add resistance exercises. Reassess in 6 months.',
  'Owner compliance excellent. Hydrotherapy sessions twice weekly for past 4 months. Muscle mass in hindquarters improving.',
  CURRENT_TIMESTAMP - INTERVAL '150 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND a.service_type_name = 'General Checkup'
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '160 days'
  AND a.appointment_date > CURRENT_TIMESTAMP - INTERVAL '200 days'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor')),
  'Max',
  (SELECT id FROM customers WHERE full_name = 'Robert Taylor'), 'Robert Taylor',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  'IgE-mediated food allergy confirmed to chicken protein. Secondary presentation: recurrent otitis externa bilaterally.',
  'Eliminate all chicken-based food and treats immediately. Switch to salmon/potato novel protein diet. Apoquel 16mg once daily for 30 days for itch management. Ear flush with Tris-EDTA + topical otic antibiotic.',
  'Serum allergy panel confirmed chicken as primary allergen; minor dust mite reaction noted. Ear cytology: Malassezia overgrowth consistent with allergic otitis.',
  CURRENT_TIMESTAMP - INTERVAL '30 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND a.service_type_name = 'Allergy Test Panel'
LIMIT 1;

-- ── MOCHI ──────────────────────────────────────────────────────────────────
INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Mochi'),
  'Mochi',
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'), 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'Suspected Chronic Kidney Disease — presenting with PU/PD, 8% weight loss over 3 months, mild lethargy.',
  'Immediate renal panel, urine SG, urinalysis. Start subcutaneous fluid support pending results.',
  'Owner reports Mochi has been drinking noticeably more water and the litter tray is fuller than usual. BCS 4/9.',
  CURRENT_TIMESTAMP - INTERVAL '530 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND a.service_type_name = 'General Checkup'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Mochi'),
  'Mochi',
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'), 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'IRIS Stage 2 Chronic Kidney Disease confirmed. BUN 52 mg/dL, Creatinine 2.8 mg/dL, SDMA 18 µg/dL. Urine SG 1.012 — isosthenuria.',
  'Renal diet (Hills k/d) exclusively. Benazepril 2.5 mg once daily (ACE inhibitor for renoprotection). Phosphorus binder with each meal. Fresh water ad libitum. Quarterly monitoring mandatory.',
  'Prognosis guarded but stable with strict dietary and medical management. Owners given full CKD management handout. Emergency signs explained: vomiting, collapse, seizure.',
  CURRENT_TIMESTAMP - INTERVAL '270 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND a.service_type_name = 'Renal Function Panel'
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '500 days'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Mochi'),
  'Mochi',
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'), 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'CKD Stage 2 — stable. Creatinine 2.6 mg/dL (improved). Weight stable at 3.8 kg. BCS 4.5/9.',
  'Continue Benazepril 2.5 mg daily. Continue renal diet and phosphorus binder. Repeat panel in 3 months.',
  'Pleasing response to dietary management. Phosphorus down from 6.8 to 5.2 mg/dL. Hydration status good. Owner compliance excellent.',
  CURRENT_TIMESTAMP - INTERVAL '90 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND a.service_type_name = 'Renal Function Panel'
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '250 days'
  AND a.appointment_date > CURRENT_TIMESTAMP - INTERVAL '300 days'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Mochi'),
  'Mochi',
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'), 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'CKD Stage 2 — stable, slight improvement. Creatinine 2.5 mg/dL. SDMA 15 µg/dL. No proteinuria.',
  'Continue current protocol unchanged. Next panel at 90 days.',
  'Mochi is maintaining weight and has good energy levels according to owner. Low-stress handling used; cat remained calm throughout examination.',
  CURRENT_TIMESTAMP + INTERVAL '90 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND a.service_type_name = 'Renal Function Panel'
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '80 days'
  AND a.appointment_date > CURRENT_TIMESTAMP - INTERVAL '120 days'
LIMIT 1;

-- ── COOPER ─────────────────────────────────────────────────────────────────
INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Cooper'),
  'Cooper',
  (SELECT id FROM customers WHERE full_name = 'Kevin Park'), 'Kevin Park',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'Healthy 8-week Labrador puppy. No congenital defects detected. BCS 5/9. Good socialization history.',
  'Start vaccination series (DA2PP, Rabies at 16 weeks). Monthly flea/tick prevention. Socialization classes recommended.',
  'Puppy is bright, alert and responsive. Excellent temperament. Heart and lungs auscultate clearly. Hernia check negative. Owner counselled on puppy-proofing, nutrition, training foundations.',
  NULL, 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Cooper')
  AND a.service_type_name = 'Wellness / Puppy Exam'
LIMIT 1;

-- ── PEBBLE ─────────────────────────────────────────────────────────────────
INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Pebble'),
  'Pebble',
  (SELECT id FROM customers WHERE full_name = 'Linda Nguyen'), 'Linda Nguyen',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  'Acute GI stasis — hypomotility confirmed on palpation and auscultation. Mild bloating. No intestinal obstruction on radiograph.',
  'Subcutaneous fluids 20 ml/kg. Metoclopramide 0.5 mg/kg SC (motility stimulant). Force-feed Critical Care formula q4h until eating independently. Remove pellets; hay only.',
  'Owner brought Pebble in within 6 hours of noticing she stopped eating — excellent owner vigilance. GI stasis in rabbits is life-threatening if not treated promptly. Full monitoring instructions given.',
  CURRENT_TIMESTAMP - INTERVAL '186 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Pebble')
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '195 days'
LIMIT 1;

INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Pebble'),
  'Pebble',
  (SELECT id FROM customers WHERE full_name = 'Linda Nguyen'), 'Linda Nguyen',
  (SELECT id FROM users WHERE username = 'dr.webb'), 'Dr. Marcus Webb',
  'Post-GI stasis recovery — fully resolved. Normal gut motility on auscultation. Eating and passing cecotropes normally.',
  'No medications required. Owner to maintain high-fibre hay-based diet, limit pellets to 1 tbsp/day. Monitor daily.',
  'Excellent recovery within 10 days. Owner educated on early warning signs: anorexia > 12 hours, reduced/no fecal output, pressing abdomen to ground.',
  NULL, 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Pebble')
  AND a.appointment_date < CURRENT_TIMESTAMP - INTERVAL '185 days'
  AND a.appointment_date > CURRENT_TIMESTAMP - INTERVAL '195 days'
LIMIT 1;

-- ── COCO ───────────────────────────────────────────────────────────────────
INSERT INTO medical_records
  (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name,
   diagnosis, treatment, notes, follow_up_date, created_by)
SELECT
  a.id,
  (SELECT id FROM pets WHERE name = 'Coco'),
  'Coco',
  (SELECT id FROM customers WHERE full_name = 'Maria Santos'), 'Maria Santos',
  (SELECT id FROM users WHERE username = 'dr.chen'), 'Dr. Sarah Chen',
  'Healthy 3-month Holland Lop. No abnormalities detected. Malocclusion check clear. Good gut sounds.',
  'Start RHDV2 vaccination series at next visit (4 months). Discuss neuter at 4–5 months.',
  'First visit for Coco. Owner well-prepared with appropriate hay, water, and housing setup. Counselled on rabbit-safe vegetables and toxic plants to avoid.',
  CURRENT_TIMESTAMP + INTERVAL '14 days', 'System'
FROM appointments a
WHERE a.pet_id = (SELECT id FROM pets WHERE name = 'Coco')
LIMIT 1;

-- =========================================================================
-- 9.  MEDICAL RECORD MEDICATIONS
-- =========================================================================

-- Max: Allergy visit → Apoquel + Doxycycline (ear infection)
INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Apoquel (Oclacitinib)'),
  '16 mg once daily for 30 days, then 16 mg every other day for maintenance',
  'Reassess itch score at 4-week recheck. Discontinue if GI side effects occur.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND mr.diagnosis ILIKE '%food allergy%'
LIMIT 1;

INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Doxycycline'),
  '5 mg/kg twice daily for 14 days',
  'For secondary bacterial otitis associated with allergic dermatitis. Give with food to reduce GI irritation.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND mr.diagnosis ILIKE '%food allergy%'
LIMIT 1;

-- Max: Hip dysplasia visit → Meloxicam + Tramadol (pain management)
INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Meloxicam'),
  '0.1 mg/kg once daily with food — use on symptomatic days only',
  'Long-term NSAID use. Monitor for GI signs (vomiting, dark stools). Annual bloodwork required.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND mr.diagnosis ILIKE '%hip dysplasia%'
LIMIT 1;

INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Tramadol'),
  '2 mg/kg twice daily for 5 days post-diagnosis',
  'Short-term pain management following diagnosis visit. Do not use long-term without bloodwork.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Max' AND customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor'))
  AND mr.diagnosis ILIKE '%hip dysplasia%'
LIMIT 1;

-- Mochi: CKD Stage 2 → Benazepril + Phosphorus Binder
INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Benazepril'),
  '2.5 mg once daily (0.5 mg/kg) — long-term',
  'ACE inhibitor for renoprotection and reduction of glomerular hypertension in CKD cats. Monitor BP annually.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND mr.diagnosis ILIKE '%Stage 2%'
  AND mr.created_at < CURRENT_TIMESTAMP - INTERVAL '250 days'
LIMIT 1;

INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Phosphorus Binder (Lante)'),
  '250 mg mixed with each meal (3× daily)',
  'Binds dietary phosphorus to limit intestinal absorption. Critical adjunct to renal diet in CKD.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Mochi')
  AND mr.diagnosis ILIKE '%Stage 2%'
  AND mr.created_at < CURRENT_TIMESTAMP - INTERVAL '250 days'
LIMIT 1;

-- Pebble: GI stasis visit → Cerenia
INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes)
SELECT
  mr.id,
  (SELECT id FROM medications WHERE name = 'Cerenia (Maropitant)'),
  '1 mg/kg SC once daily for 3 days',
  'Anti-nausea and gut motility support during GI stasis episode. Monitor for SC site reaction.'
FROM medical_records mr
WHERE mr.pet_id = (SELECT id FROM pets WHERE name = 'Pebble')
  AND mr.diagnosis ILIKE '%GI stasis%'
LIMIT 1;

-- =========================================================================
-- 10. CBC RECORDS
-- =========================================================================

-- ── MAX CBC ────────────────────────────────────────────────────────────────
-- Baseline: normal
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  CURRENT_TIMESTAMP - INTERVAL '730 days',
  7.10, 17.2, 51.0, 71.8, 24.2, 33.7, 298, 9.8, 68, 22, 5, 4, 1,
  'Baseline CBC — all parameters within normal canine reference intervals.',
  'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

-- During dysplasia workup: mild inflammatory leukocytosis
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  CURRENT_TIMESTAMP - INTERVAL '680 days',
  6.95, 16.8, 49.5, 71.2, 24.2, 34.0, 312, 14.8, 74, 18, 5, 2, 1,
  'WBC mildly elevated (14.8 × 10⁹/L) — stress leukogram vs early inflammatory response. Neutrophilia without left shift.',
  'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

-- 6-month: allergy / eosinophilia
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Max', p.customer_id, 'Robert Taylor',
  CURRENT_TIMESTAMP - INTERVAL '60 days',
  7.05, 17.0, 50.2, 71.4, 24.1, 33.9, 285, 11.2, 58, 24, 4, 13, 1,
  'Eosinophilia (13%) consistent with allergic/parasitic disease — correlates with allergy test results. Monitor following diet change and Apoquel therapy.',
  'System'
FROM pets p WHERE p.name = 'Max' AND p.customer_id = (SELECT id FROM customers WHERE full_name = 'Robert Taylor');

-- ── MOCHI CBC ──────────────────────────────────────────────────────────────
-- At CKD diagnosis: mild non-regenerative anaemia
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  CURRENT_TIMESTAMP - INTERVAL '535 days',
  5.10, 9.8, 28.5, 43.5, 19.2, 34.4, 380, 12.8, 66, 26, 5, 2, 1,
  'Mild non-regenerative anaemia (HCT 28.5% — below feline ref 30–45%). Consistent with CKD-associated erythropoietin deficiency. HGB 9.8 g/dL below reference. Monitor trend.',
  'System'
FROM pets p WHERE p.name = 'Mochi';

-- 6-month follow-up: anaemia worsening
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  CURRENT_TIMESTAMP - INTERVAL '270 days',
  4.85, 9.2, 27.0, 44.1, 19.0, 34.1, 365, 11.5, 64, 27, 6, 2, 1,
  'HCT 27% — anaemia progressing slightly. HGB 9.2 g/dL (below ref 12–18 g/dL). Consider erythropoiesis-stimulating agents if HCT drops below 20%. Dietary iron supplementation discussed.',
  'System'
FROM pets p WHERE p.name = 'Mochi';

-- 9-month follow-up: slight improvement with management
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Mochi', p.customer_id, 'Maria Santos',
  CURRENT_TIMESTAMP - INTERVAL '90 days',
  5.02, 9.9, 29.8, 44.0, 19.7, 33.9, 372, 10.8, 62, 29, 5, 3, 1,
  'HCT 29.8% — marginal improvement. Benazepril may be improving renal perfusion slightly. HGB 9.9 g/dL still below reference but trending up. Continue protocol and re-evaluate.',
  'System'
FROM pets p WHERE p.name = 'Mochi';

-- ── COOPER CBC ────────────────────────────────────────────────────────────
-- Baseline puppy CBC
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Cooper', p.customer_id, 'Kevin Park',
  CURRENT_TIMESTAMP - INTERVAL '100 days',
  6.20, 14.5, 43.0, 69.4, 23.4, 33.7, 440, 13.5, 58, 32, 5, 4, 1,
  'Healthy puppy baseline. Lymphocytosis (32%) — physiological in young dogs. WBC 13.5 is within normal puppy range. All erythrocyte parameters normal.',
  'System'
FROM pets p WHERE p.name = 'Cooper';

-- ── PEBBLE CBC ────────────────────────────────────────────────────────────
-- During GI stasis emergency
INSERT INTO cbc_records
  (pet_id, pet_name, customer_id, customer_name, test_date,
   rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by)
SELECT
  p.id, 'Pebble', p.customer_id, 'Linda Nguyen',
  CURRENT_TIMESTAMP - INTERVAL '200 days',
  6.40, 13.8, 42.5, 66.4, 21.6, 32.5, 510, 19.5, 78, 14, 5, 2, 1,
  'Leukocytosis (WBC 19.5 × 10⁹/L) — stress response and possible endotoxemia from GI stasis. Neutrophilia with mild left shift (band neutrophils 4%). PLT 510 — mild thrombocytosis, reactive. Immediate fluid and supportive therapy initiated.',
  'System'
FROM pets p WHERE p.name = 'Pebble';

-- =========================================================================
-- END OF DEMO DATA SCRIPT
-- =========================================================================
SELECT
  'Loaded: ' ||
  (SELECT COUNT(*) FROM users WHERE username IN ('dr.chen','dr.webb'))::TEXT || ' vet users, ' ||
  (SELECT COUNT(*) FROM pets WHERE name IN ('Max','Mochi','Cooper','Pebble','Coco'))::TEXT || ' demo pets, ' ||
  (SELECT COUNT(*) FROM medical_records mr JOIN pets p ON p.id = mr.pet_id WHERE p.name IN ('Max','Mochi','Cooper','Pebble','Coco'))::TEXT || ' medical records, ' ||
  (SELECT COUNT(*) FROM cbc_records cr JOIN pets p ON p.id = cr.pet_id WHERE p.name IN ('Max','Mochi','Cooper','Pebble','Coco'))::TEXT || ' CBC records loaded.' AS result;

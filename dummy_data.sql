-- =========================================================================
-- VET-MS DUMMY DATA SEED SCRIPT
-- Run this in your PostgreSQL database to populate the missing forms.
-- =========================================================================

-- 1. Medications
INSERT INTO public.medications (name, category, dosage_form, unit, description, is_active) VALUES
('Amoxicillin', 'Antibiotic', 'Tablet', '250mg', 'Broad-spectrum antibiotic for bacterial infections.', true),
('Rimadyl (Carprofen)', 'NSAID', 'Chewable', '75mg', 'Non-steroidal anti-inflammatory drug for pain relief.', true),
('Cerenia (Maropitant)', 'Antiemetic', 'Injectable', '10mg/ml', 'Used to treat and prevent vomiting in dogs and cats.', true),
('Apoquel', 'Dermatological', 'Tablet', '5.4mg', 'Control of pruritus associated with allergic dermatitis.', true),
('Bravecto', 'Parasiticide', 'Chewable', '500mg', 'Flea and tick prevention lasting up to 12 weeks.', true),
('Heartgard Plus', 'Parasiticide', 'Chewable', '68mcg', 'Prevents heartworm disease and treats roundworms/hookworms.', true),
('Metronidazole', 'Antibiotic', 'Tablet', '250mg', 'Effective against anaerobic bacteria and certain parasites.', true),
('Gabapentin', 'Neurological', 'Capsule', '100mg', 'Used for seizures and chronic neuropathic pain management.', true),
('Prednisone', 'Corticosteroid', 'Tablet', '10mg', 'Immunosuppressant and anti-inflammatory properties.', true),
('Depo-Medrol', 'Corticosteroid', 'Injectable', '20mg/ml', 'Long-acting injectable steroid for severe inflammation.', true);

-- 2. Service Types
INSERT INTO public.service_types (name, category, price, description, is_active) VALUES
('General Consultation', 'Consultation', 45.00, 'Basic detailed physical examination and consultation.', true),
('Emergency Visit', 'Consultation', 120.00, 'Walk-in or after-hours emergency physical examination.', true),
('Annual Vaccination DAPPL', 'Preventative', 35.00, 'Routine canine annual booster shot for 5 major diseases.', true),
('Rabies Vaccination (1 Year)', 'Preventative', 20.00, 'Standard 1-year rabies vaccination including certificate.', true),
('Nail Trim', 'Grooming', 20.00, 'Standard painless nail clipping and filing.', true),
('Dental Cleaning (Level 1)', 'Dental', 180.00, 'Full scaler, polish, and fluoride treatment under general anesthesia.', true),
('Spay/Neuter (Feline)', 'Surgery', 150.00, 'Routine surgical sterilization for cats.', true),
('Microchipping', 'Identification', 40.00, 'Implantation of ISO standard microchip and lifetime registration.', true),
('Complete Blood Count (CBC)', 'Diagnostic', 85.00, 'Standard in-house blood cytology panel.', true),
('X-Ray (2 Views)', 'Diagnostic', 140.00, 'Digital radiography with radiological interpretation.', true);

-- 3. Suppliers
INSERT INTO public.suppliers (company_name, contact_person, phone, email, address, is_active) VALUES
('PharmaVet Distribution', 'Arthur Pendelton', '1-800-555-0123', 'orders@pharmavet.com', '123 Medical Way, Warehouse District, TX', true),
('Paws & Claws Supply Corp.', 'Sarah Jenkins', '1-800-555-0199', 'sales@pawsclaws.com', '456 Industrial Blvd, Building A, CA', true),
('Global Vet Technologies', 'Michael Chen', '1-888-555-4422', 'michael.c@globalvet.tech', '789 Innovation Parkway, FL', true),
('Surgical Needs LLC', 'Diana Ross', '1-800-555-8811', 'diana@surgicalneeds.net', '321 Scalpel Drive, NY', true),
('Cornerstone Nutrition', 'Robert Hale', '1-800-555-3344', 'admin@cornerstonenutrition.com', '98 Pet Food Lane, OH', true);


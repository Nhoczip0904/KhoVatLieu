import { useState, useEffect } from 'react';

export const useStorage = (key, initialValue) => {
  const [value, setValue] = useState(() => {
    const saved = localStorage.getItem(key);
    if (saved) return JSON.parse(saved);
    return initialValue;
  });

  useEffect(() => {
    localStorage.setItem(key, JSON.stringify(value));
  }, [key, value]);

  return [value, setValue];
};

export const DATA_KEYS = {
  MASTER_MATERIALS: 'gh_master_materials',
  PROJECTS: 'gh_projects',
  DELIVERIES: 'gh_deliveries'
};
